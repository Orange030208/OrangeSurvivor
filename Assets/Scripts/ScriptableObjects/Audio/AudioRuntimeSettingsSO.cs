using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频运行时设置资源：
/// - 提供 BGM 淡入淡出时长；
/// - 直接维护 AudioBgmKey 到具体音频配置的映射。
/// 该资源只负责音乐 Cue 配置，不负责游戏状态驱动与音量控制。
/// </summary>
[CreateAssetMenu(fileName = "Audio Runtime Settings", menuName = ScriptableObjectMenuPaths.AUDIO_RUNTIME_SETTINGS, order = 1)]
public class AudioRuntimeSettingsSO : ScriptableObject
{
    [Header("背景音乐")]
    [Tooltip("背景音乐切换与停止时使用的淡入淡出时长。0 表示立即切换。")]
    [SerializeField] [Min(AudioConstants.MIN_FADE_DURATION)] private float musicFadeDuration = AudioConstants.DEFAULT_MUSIC_FADE_DURATION;
    [Tooltip("背景音乐配置列表。每个枚举键直接对应一个具体音频配置。")]
    [SerializeField] private AudioBgmEntry[] bgmCues = Array.Empty<AudioBgmEntry>();

    private Dictionary<AudioBgmKey, AudioCueData> bgmCueCache;

    public float MusicFadeDuration => musicFadeDuration;

    /// <summary>
    /// 按 BGM 键查询具体音频配置。
    /// </summary>
    public bool TryGetBgmCue(AudioBgmKey bgmKey, out AudioCueData cueData)
    {
        cueData = default;
        if (bgmKey == AudioBgmKey.None)
        {
            return false;
        }

        EnsureBgmCueCache();
        return bgmCueCache.TryGetValue(bgmKey, out cueData);
    }

    private void OnValidate()
    {
        musicFadeDuration = Mathf.Max(AudioConstants.MIN_FADE_DURATION, musicFadeDuration);

        if (bgmCues == null)
        {
            bgmCues = Array.Empty<AudioBgmEntry>();
        }

        for (int i = 0; i < bgmCues.Length; i++)
        {
            bgmCues[i]?.OnValidate();
        }

        bgmCueCache = null;
    }

    private void EnsureBgmCueCache()
    {
        if (bgmCueCache != null)
        {
            return;
        }

        bgmCueCache = new Dictionary<AudioBgmKey, AudioCueData>();
        for (int i = 0; i < bgmCues.Length; i++)
        {
            AudioBgmEntry cueEntry = bgmCues[i];
            if (cueEntry == null || !cueEntry.TryBuild(out AudioCueData cueData))
            {
                continue;
            }

            bgmCueCache[cueEntry.BgmKey] = cueData;
        }
    }
}
