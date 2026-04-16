using System;
using UnityEngine;

/// <summary>
/// 单条语义音效映射：
/// - 定义一个 AudioSfxKey；
/// - 映射到具体 cueId；
/// - 供 AudioSfxCatalogSO 在运行时查询。
/// </summary>
[Serializable]
public class AudioSfxEntry
{
    [Tooltip("语义化音效键。调用方应依赖这个键，而不是直接依赖 cueId 字符串。")]
    [SerializeField] private AudioSfxKey sfxKey = AudioSfxKey.UiClick;
    [Tooltip("该语义音效对应的具体 cueId。")]
    [SerializeField] private string cueId = AudioConstants.DEFAULT_UI_CLICK_CUE_ID;

    public AudioSfxKey SfxKey => sfxKey;
    public string CueId => cueId;

    public bool TryBuild(out AudioSfxKey key, out string resolvedCueId)
    {
        key = sfxKey;
        resolvedCueId = cueId;
        return sfxKey != AudioSfxKey.None && !string.IsNullOrWhiteSpace(cueId);
    }

    public void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            cueId = AudioConstants.DEFAULT_UI_CLICK_CUE_ID;
            return;
        }

        if (!cueId.StartsWith(AudioConstants.CUE_ID_PREFIX, StringComparison.Ordinal))
        {
            cueId = AudioConstants.CUE_ID_PREFIX + cueId;
        }
    }
}
