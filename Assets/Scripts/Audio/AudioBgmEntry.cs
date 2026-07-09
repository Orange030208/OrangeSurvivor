using System;
using UnityEngine;

/// <summary>
/// 单条背景音乐配置。
/// 只描述音乐键、音频资源、播放模式与音高；音量统一由 AudioService 管理。
/// </summary>
[Serializable]
public class AudioBgmEntry
{
    [Tooltip("语义化背景音乐键。调用方直接依赖该枚举播放。")]
    [SerializeField] private AudioBgmKey bgmKey = AudioBgmKey.Menu;
    [Tooltip("该背景音乐对应的音频资源。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("可选随机候选曲目。为空时仅播放主音频资源；非空时会按权重随机选择候选曲目，主音频资源作为兜底。")]
    [SerializeField] private AudioSfxClipVariant[] clipVariants = Array.Empty<AudioSfxClipVariant>();
    [Tooltip("播放模式。背景音乐默认建议使用循环播放。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.Loop;
    [Tooltip("该背景音乐的基础音高。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;

    public AudioBgmKey BgmKey => bgmKey;

    public bool TryBuild(out AudioCueData cueData)
    {
        cueData = default;
        AudioClip resolvedClip = ResolvePrimaryClip();
        if (bgmKey == AudioBgmKey.None || resolvedClip == null)
        {
            return false;
        }

        cueData = new AudioCueData(
            bgmKey.ToString(),
            resolvedClip,
            AudioBusType.Music,
            playbackMode,
            pitch,
            AudioConstants.DEFAULT_VOLUME,
            AudioConstants.DEFAULT_SFX_GROUP_ID,
            AudioConstants.DEFAULT_CUE_MAX_CONCURRENT,
            0f,
            AudioConstants.DEFAULT_AUDIO_PRIORITY,
            false,
            clipVariants ?? Array.Empty<AudioSfxClipVariant>());
        return true;
    }

    public void OnValidate()
    {
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);

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
}
