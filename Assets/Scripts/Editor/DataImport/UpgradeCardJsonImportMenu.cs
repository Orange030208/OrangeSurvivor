#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UpgradeCardJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Upgrade Cards From JSON")]
    public static void PreviewUpgradeCards()
    {
        IReadOnlyList<UpgradeCardJsonCard> cards = UpgradeCardJsonReader.ReadDefault();
        DataImportReport report = UpgradeCardJsonAssetSync.Preview(cards);
        Debug.Log($"Upgrade card JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Upgrade Cards From JSON")]
    public static void ApplyUpgradeCards()
    {
        IReadOnlyList<UpgradeCardJsonCard> cards = UpgradeCardJsonReader.ReadDefault();
        DataImportReport preview = UpgradeCardJsonAssetSync.Preview(cards);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Upgrade card JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = UpgradeCardJsonAssetSync.Apply(cards);
        AssetDatabase.Refresh();
        Debug.Log($"Upgrade card JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
