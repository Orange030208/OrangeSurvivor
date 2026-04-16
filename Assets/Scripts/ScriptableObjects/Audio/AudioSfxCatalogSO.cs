using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 语义音效映射总表：
/// - 维护 AudioSfxKey 到 cueId 的映射；
/// - 让 UI 与业务层不再直接依赖音频资源字符串；
/// - 只负责配置索引与缓存，不参与实际播放。
/// </summary>
[CreateAssetMenu(fileName = "Audio Sfx Catalog", menuName = AudioConstants.AUDIO_SFX_CATALOG_MENU_PATH, order = 2)]
public class AudioSfxCatalogSO : ScriptableObject
{
    [Tooltip("语义音效映射列表。每个键最终会映射到一个具体 cueId。")]
    [SerializeField] private AudioSfxEntry[] entries = Array.Empty<AudioSfxEntry>();

    private Dictionary<AudioSfxKey, string> cache;

    /// <summary>
    /// 按语义音效键查询具体 cueId。
    /// </summary>
    public bool TryGetCueId(AudioSfxKey sfxKey, out string cueId)
    {
        cueId = null;
        if (sfxKey == AudioSfxKey.None)
        {
            return false;
        }

        EnsureCache();
        return cache.TryGetValue(sfxKey, out cueId) && !string.IsNullOrWhiteSpace(cueId);
    }

    private void OnValidate()
    {
        if (entries == null)
        {
            entries = Array.Empty<AudioSfxEntry>();
            cache = null;
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            entries[i]?.OnValidate();
        }

        cache = null;
    }

    private void EnsureCache()
    {
        if (cache != null)
        {
            return;
        }

        cache = new Dictionary<AudioSfxKey, string>();
        for (int i = 0; i < entries.Length; i++)
        {
            AudioSfxEntry entry = entries[i];
            if (entry == null || !entry.TryBuild(out AudioSfxKey key, out string cueId))
            {
                continue;
            }

            cache[key] = cueId;
        }
    }
}
