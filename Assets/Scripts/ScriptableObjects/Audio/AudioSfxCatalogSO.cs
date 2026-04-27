using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音效配置总表：
/// - 直接维护 AudioSfxKey 到具体音频配置的映射；
/// - 让 UI 与业务层只依赖强类型枚举；
/// - 只负责配置索引与缓存，不参与实际播放。
/// </summary>
[CreateAssetMenu(fileName = "Audio Sfx Catalog", menuName = ScriptableObjectMenuPaths.AUDIO_SFX_CATALOG, order = 2)]
public class AudioSfxCatalogSO : ScriptableObject
{
    [Tooltip("音效配置列表。每个枚举键直接对应一个具体音频配置。")]
    [SerializeField] private AudioSfxEntry[] entries = Array.Empty<AudioSfxEntry>();

    private Dictionary<AudioSfxKey, AudioCueData> cache;

    /// <summary>
    /// 按语义音效键查询具体音频配置。
    /// </summary>
    public bool TryGetCue(AudioSfxKey sfxKey, out AudioCueData cueData)
    {
        cueData = default;
        if (sfxKey == AudioSfxKey.None)
        {
            return false;
        }

        EnsureCache();
        return cache.TryGetValue(sfxKey, out cueData);
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

        cache = new Dictionary<AudioSfxKey, AudioCueData>();
        for (int i = 0; i < entries.Length; i++)
        {
            AudioSfxEntry entry = entries[i];
            if (entry == null || !entry.TryBuild(out AudioCueData cueData))
            {
                continue;
            }

            cache[entry.SfxKey] = cueData;
        }
    }
}
