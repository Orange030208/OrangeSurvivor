#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AccessoryJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Accessories From JSON")]
    public static void PreviewAccessories()
    {
        IReadOnlyList<AccessoryJsonAccessory> accessories = AccessoryJsonReader.ReadDefault();
        DataImportReport report = AccessoryJsonAssetSync.Preview(accessories);
        Debug.Log($"Accessory JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Accessories From JSON")]
    public static void ApplyAccessories()
    {
        IReadOnlyList<AccessoryJsonAccessory> accessories = AccessoryJsonReader.ReadDefault();
        DataImportReport preview = AccessoryJsonAssetSync.Preview(accessories);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Accessory JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = AccessoryJsonAssetSync.Apply(accessories);
        AssetDatabase.Refresh();
        Debug.Log($"Accessory JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
