using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频运行时设置资源：
/// - 提供 GameState 到 BGM 的映射；
/// - 提供 BGM 淡入淡出时长。
/// 该资源只负责状态驱动的 BGM 配置，不负责音量控制。
/// </summary>
[CreateAssetMenu(fileName = "Audio Runtime Settings", menuName = AudioConstants.AUDIO_RUNTIME_SETTINGS_MENU_PATH, order = 1)]
public class AudioRuntimeSettingsSO : ScriptableObject
{
    [Header("BGM")]
    [Tooltip("BGM 切换与停止时使用的淡入淡出时长。0 表示立即切换。")]
    [SerializeField] [Min(AudioConstants.MIN_FADE_DURATION)] private float musicFadeDuration = AudioConstants.DEFAULT_MUSIC_FADE_DURATION;
    [Tooltip("GameState 到 BGM 配置的映射表。可通过关闭 restartIfAlreadyPlaying 来跨界面延续当前音乐。")]
    [SerializeField] private AudioGameStateBgmEntry[] bgmEntries = Array.Empty<AudioGameStateBgmEntry>();

    private Dictionary<GameState, AudioGameStateBgmEntry> bgmCache;

    public float MusicFadeDuration => musicFadeDuration;

    /// <summary>
    /// 查询指定游戏状态对应的 BGM 配置。
    /// </summary>
    public bool TryGetGameStateBgmEntry(GameState gameState, out AudioGameStateBgmEntry entry)
    {
        EnsureBgmCache();
        return bgmCache.TryGetValue(gameState, out entry) && entry != null;
    }

    private void OnValidate()
    {
        musicFadeDuration = Mathf.Max(AudioConstants.MIN_FADE_DURATION, musicFadeDuration);

        if (bgmEntries == null)
        {
            bgmEntries = Array.Empty<AudioGameStateBgmEntry>();
        }

        for (int i = 0; i < bgmEntries.Length; i++)
        {
            bgmEntries[i]?.OnValidate();
        }

        bgmCache = null;
    }

    private void EnsureBgmCache()
    {
        if (bgmCache != null)
        {
            return;
        }

        bgmCache = new Dictionary<GameState, AudioGameStateBgmEntry>();
        for (int i = 0; i < bgmEntries.Length; i++)
        {
            AudioGameStateBgmEntry entry = bgmEntries[i];
            if (entry == null)
            {
                continue;
            }

            bgmCache[entry.GameState] = entry;
        }
    }
}
