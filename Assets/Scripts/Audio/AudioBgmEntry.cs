using System;
using UnityEngine;

/// <summary>
/// 单条背景音乐配置：
/// - 直接以 AudioBgmKey 作为唯一播放键；
/// - 绑定具体 AudioClip；
/// - 默认输出到 Music 总线；
/// - 指定播放模式、音量与音高。
/// 它只负责数据描述，不直接参与播放。
/// </summary>
[Serializable]
public class AudioBgmEntry
{
    [Tooltip("语义化背景音乐键。调用方直接依赖该枚举播放。")]
    [SerializeField] private AudioBgmKey bgmKey = AudioBgmKey.Menu;
    [Tooltip("该背景音乐对应的音频资源。")]
    [SerializeField] private AudioClip clip;
    [Tooltip("播放模式。BGM 默认建议使用 Loop。")]
    [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.Loop;
    [Tooltip("该背景音乐的基础音量。最终输出会再叠加总线音量。")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float volume = AudioConstants.DEFAULT_VOLUME;
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

        cueData = new AudioCueData(bgmKey.ToString(), clip, AudioBusType.Music, playbackMode, volume, pitch);
        return true;
    }

    public void OnValidate()
    {
        volume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
}
