#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuffJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Buffs From JSON")]
    public static void PreviewBuffs()
    {
        IReadOnlyList<BuffJsonBuff> buffs = BuffJsonReader.ReadDefault();
        DataImportReport report = BuffJsonAssetSync.Preview(buffs);
        Debug.Log($"Buff JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Buffs From JSON")]
    public static void ApplyBuffs()
    {
        IReadOnlyList<BuffJsonBuff> buffs = BuffJsonReader.ReadDefault();
        DataImportReport preview = BuffJsonAssetSync.Preview(buffs);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Buff JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = BuffJsonAssetSync.Apply(buffs);
        AssetDatabase.Refresh();
        Debug.Log($"Buff JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
