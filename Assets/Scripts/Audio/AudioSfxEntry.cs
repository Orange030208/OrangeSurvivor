using System;
using UnityEngine;

/// <summary>
/// 单条音效配置：
/// - 直接以 AudioSfxKey 作为唯一播放键；
/// - 绑定具体 AudioClip；
/// - 指定目标总线、播放模式、音量与音高。
/// 它只负责数据描述，不直接参与播放。
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
    [Tooltip("该音效的基础音量。最终输出会再叠加总线音量。")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float volume = AudioConstants.DEFAULT_VOLUME;
    [Tooltip("该音效的基础音高。")]
    [SerializeField] [Range(AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH)] private float pitch = AudioConstants.DEFAULT_PITCH;

    public AudioSfxKey SfxKey => sfxKey;

    /// <summary>
    /// 尝试把 Inspector 配置构造成运行时可用的 AudioCueData。
    /// </summary>
    public bool TryBuild(out AudioCueData cueData)
    {
        cueData = default;
        if (sfxKey == AudioSfxKey.None || clip == null)
        {
            return false;
        }

        cueData = new AudioCueData(sfxKey.ToString(), clip, busType, playbackMode, volume, pitch);
        return true;
    }

    public void OnValidate()
    {
        volume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
}
