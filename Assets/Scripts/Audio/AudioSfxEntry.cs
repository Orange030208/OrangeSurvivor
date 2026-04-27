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
    [Tooltip("该音效对应的音频资源。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("该音效默认输出到哪个音频总线。")]
    [SerializeField] private AudioBusType busType = AudioBusType.Sfx;
    [Tooltip("播放模式。OneShot 适合按钮/命中音效，Loop 适合少量循环音效。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.OneShot;
    [Tooltip("该音效的基础音高。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;

    public AudioSfxKey SfxKey => sfxKey;

    public bool TryBuild(out AudioCueData cueData)
    {
        cueData = default;
        if (sfxKey == AudioSfxKey.None || clip == null)
        {
            return false;
        }

        cueData = new AudioCueData(sfxKey.ToString(), clip, busType, playbackMode, pitch);
        return true;
    }

    public void OnValidate()
    {
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
}
