using System;
using UnityEngine;

/// <summary>
/// 单条音效配置。
/// 只描述音效键、音频资源、目标总线、播放模式与音高；音量统一由 AudioManager 管理。
/// </summary>
[Serializable]
public class AudioSfxEntry
{
    [Tooltip("语义化音效键。调用方直接依赖该枚举播放。")]
    [SerializeField] private AudioSfxKey sfxKey = AudioSfxKey.None;
    [Tooltip("兼容旧配置的主音频资源。新配置可继续使用它作为默认变体。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("兼容旧配置的总线字段。SFX 分组现在由 groupId 决定。")]
    [SerializeField] private AudioBusType busType = AudioBusType.Sfx;
    [Tooltip("播放模式。OneShot 适合按钮/命中音效，Loop 适合少量循环音效。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.OneShot;
    [Tooltip("兼容旧配置的基础音高。若 pitchMin/pitchMax 未单独调整，将使用该值。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;
    [Tooltip("自定义 SFX 分组 ID。为空时会按语义键回退到基础分组。")]
    [SerializeField] private string groupId;
    [Tooltip("用于素材响度校准的基础倍率，不作为玩家可见的单独音效音量。")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float baseVolumeScale = AudioConstants.DEFAULT_VOLUME;
    [Tooltip("音高随机范围下限。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitchMin = AudioConstants.DEFAULT_PITCH;
    [Tooltip("音高随机范围上限。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitchMax = AudioConstants.DEFAULT_PITCH;
    [Tooltip("同一个 Cue 的最大同时播放数量。")]
    [SerializeField] [Range(AudioConstants.MIN_CONCURRENT_COUNT, AudioConstants.MAX_CONCURRENT_COUNT)] private int maxConcurrent = AudioConstants.DEFAULT_CUE_MAX_CONCURRENT;
    [Tooltip("同一个 Cue 的最小播放间隔，用于限制高频重复音效。")]
    [SerializeField] [Min(0f)] private float cooldown;
    [Tooltip("Cue 优先级。数值越小越重要，会映射到 Unity AudioSource priority。")]
    [SerializeField] [Range(0, 256)] private int priority = AudioConstants.DEFAULT_AUDIO_PRIORITY;
    [Tooltip("启用后，带位置的播放请求会按 2D 距离做音量衰减与左右声像。")]
    [SerializeField] private bool use2DSpatialBlend;
    [Tooltip("可选随机变体。为空时仅播放主音频资源。")]
    [SerializeField] private AudioSfxClipVariant[] clipVariants = Array.Empty<AudioSfxClipVariant>();

    public AudioSfxKey SfxKey => sfxKey;

    public bool TryBuild(out AudioCueData cueData)
    {
        return TryBuild(ResolveGroupId(), out cueData);
    }

    public bool TryBuild(string owningGroupId, out AudioCueData cueData)
    {
        cueData = default;
        AudioClip resolvedClip = ResolvePrimaryClip();
        if (sfxKey == AudioSfxKey.None || resolvedClip == null)
        {
            return false;
        }

        cueData = new AudioCueData(
            sfxKey.ToString(),
            resolvedClip,
            ResolvePlaybackBusType(),
            playbackMode,
            pitch,
            baseVolumeScale,
            string.IsNullOrWhiteSpace(owningGroupId) ? ResolveGroupId() : owningGroupId.Trim(),
            maxConcurrent,
            cooldown,
            priority,
            use2DSpatialBlend,
            clipVariants ?? Array.Empty<AudioSfxClipVariant>(),
            pitchMin,
            pitchMax);
        return true;
    }

    public void OnValidate()
    {
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        baseVolumeScale = Mathf.Clamp(baseVolumeScale, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        pitchMin = Mathf.Clamp(pitchMin, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        pitchMax = Mathf.Clamp(pitchMax, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        if (pitchMin > pitchMax)
        {
            (pitchMin, pitchMax) = (pitchMax, pitchMin);
        }

        maxConcurrent = Mathf.Clamp(maxConcurrent, AudioConstants.MIN_CONCURRENT_COUNT, AudioConstants.MAX_CONCURRENT_COUNT);
        cooldown = Mathf.Max(0f, cooldown);
        priority = Mathf.Clamp(priority, 0, 256);
        groupId = string.IsNullOrWhiteSpace(groupId) ? string.Empty : groupId.Trim();

        if (clipVariants == null)
        {
            clipVariants = Array.Empty<AudioSfxClipVariant>();
        }

        for (int i = 0; i < clipVariants.Length; i++)
        {
            clipVariants[i]?.OnValidate();
        }
    }

    private AudioClip ResolvePrimaryClip()
    {
        if (clip != null)
        {
            return clip;
        }

        if (clipVariants == null)
        {
            return null;
        }

        for (int i = 0; i < clipVariants.Length; i++)
        {
            if (clipVariants[i] != null && clipVariants[i].IsValid)
            {
                return clipVariants[i].Clip;
            }
        }

        return null;
    }

    private string ResolveGroupId()
    {
        if (!string.IsNullOrWhiteSpace(groupId))
        {
            return groupId.Trim();
        }

        int keyValue = (int)sfxKey;
        if (keyValue >= 100 && keyValue < 200 || sfxKey == AudioSfxKey.WoodenButtonClicked)
        {
            return AudioConstants.UI_SFX_GROUP_ID;
        }

        if (keyValue >= 200 && keyValue < 300)
        {
            return AudioConstants.PICKUP_SFX_GROUP_ID;
        }

        if (keyValue >= 300 && keyValue < 500 || sfxKey == AudioSfxKey.Swipe || sfxKey == AudioSfxKey.Slap)
        {
            return AudioConstants.COMBAT_SFX_GROUP_ID;
        }

        return AudioConstants.DEFAULT_SFX_GROUP_ID;
    }

    private AudioBusType ResolvePlaybackBusType()
    {
        return busType == AudioBusType.Music ? AudioBusType.Sfx : busType;
    }
}
