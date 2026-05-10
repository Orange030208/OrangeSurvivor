using System;
using UnityEngine;

/// <summary>
/// 单条背景音乐配置。
/// 只描述音乐键、音频资源、播放模式与音高；音量统一由 AudioManager 管理。
/// </summary>
[Serializable]
public class AudioBgmEntry
{
    [Tooltip("语义化背景音乐键。调用方直接依赖该枚举播放。")]
    [SerializeField] private AudioBgmKey bgmKey = AudioBgmKey.Menu;
    [Tooltip("该背景音乐对应的音频资源。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("播放模式。背景音乐默认建议使用循环播放。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.Loop;
    [Tooltip("该背景音乐的基础音高。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;

    public AudioBgmKey BgmKey => bgmKey;

    public bool TryBuild(out AudioCueData cueData)
    {
        cueData = default;
        if (bgmKey == AudioBgmKey.None || clip == null)
        {
            return false;
        }

        cueData = new AudioCueData(bgmKey.ToString(), clip, AudioBusType.Music, playbackMode, pitch);
        return true;
    }

    public void OnValidate()
    {
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
}
