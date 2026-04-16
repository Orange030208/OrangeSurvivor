using System;
using UnityEngine;

/// <summary>
/// 单条音频 cue 配置：
/// - 定义 cueId；
/// - 绑定具体 AudioClip；
/// - 指定目标总线、播放模式、音量与音高。
/// 它只负责数据描述，不直接参与播放。
/// </summary>
[Serializable]
public class AudioCueEntry
{
    [Tooltip("音频条目的唯一标识。建议使用带模块前缀的形式，例如 Audio_UI_Click。")]
    [SerializeField] private string cueId = AudioConstants.DEFAULT_CUE_ID;
    [Tooltip("该 cue 对应的音频资源。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("该 cue 默认输出到哪个音频总线。")]
    [SerializeField] private AudioBusType busType = AudioBusType.Sfx;
    [Tooltip("播放模式。OneShot 适合按钮/命中音效，Loop 适合 BGM。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.OneShot;
    [Tooltip("该 cue 的基础音量。最终输出会再叠加总线音量。")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float volume = AudioConstants.DEFAULT_VOLUME;
    [Tooltip("该 cue 的基础音高。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;

    public string CueId => cueId;

    /// <summary>
    /// 尝试把 Inspector 配置构造成运行时可用的 AudioCueData。
    /// </summary>
    public bool TryBuild(out AudioCueData cueData)
    {
        cueData = default;
        if (string.IsNullOrWhiteSpace(cueId) || clip == null)
        {
            return false;
        }

        cueData = new AudioCueData(cueId, clip, busType, playbackMode, volume, pitch);
        return true;
    }

    public void OnValidate()
    {
        volume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);

        if (string.IsNullOrWhiteSpace(cueId))
        {
            cueId = AudioConstants.DEFAULT_CUE_ID;
        }
        else if (!cueId.StartsWith(AudioConstants.CUE_ID_PREFIX, StringComparison.Ordinal))
        {
            cueId = AudioConstants.CUE_ID_PREFIX + cueId;
        }
    }
}
