using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频 cue 总表资源：
/// - 统一登记所有可播放的音频条目；
/// - 通过 cueId 提供运行时查询；
/// - 只负责配置索引与缓存，不参与播放逻辑。
/// </summary>
[CreateAssetMenu(fileName = "Audio Cue Catalog", menuName = AudioConstants.AUDIO_CATALOG_MENU_PATH, order = 0)]
public class AudioCueCatalogSO : ScriptableObject
{
    [Tooltip("音频条目列表。每个条目对应一个 cueId，供 UI/业务层通过字符串发起播放请求。")]
    [SerializeField] private AudioCueEntry[] cues = Array.Empty<AudioCueEntry>();

    private Dictionary<string, AudioCueData> cache;

    /// <summary>
    /// 按 cueId 查询音频配置。
    /// </summary>
    public bool TryGetCue(string cueId, out AudioCueData cueData)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            cueData = default;
            return false;
        }

        EnsureCache();
        return cache.TryGetValue(cueId, out cueData);
    }

    private void OnValidate()
    {
        if (cues == null)
        {
            cues = Array.Empty<AudioCueEntry>();
            cache = null;
            return;
        }

        for (int i = 0; i < cues.Length; i++)
        {
            if (cues[i] == null)
            {
                continue;
            }

            cues[i].OnValidate();
        }

        cache = null;
    }

    private void EnsureCache()
    {
        if (cache != null)
        {
            return;
        }

        cache = new Dictionary<string, AudioCueData>(StringComparer.Ordinal);
        for (int i = 0; i < cues.Length; i++)
        {
            AudioCueEntry cue = cues[i];
            if (cue == null || !cue.TryBuild(out AudioCueData cueData))
            {
                continue;
            }

            cache[cueData.CueId] = cueData;
        }
    }
}
