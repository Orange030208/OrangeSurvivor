#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WeaponJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Weapons From JSON")]
    public static void PreviewWeapons()
    {
        IReadOnlyList<WeaponJsonWeapon> weapons = WeaponJsonReader.ReadDefault();
        DataImportReport report = WeaponJsonAssetSync.Preview(weapons);
        Debug.Log($"Weapon JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Weapons From JSON")]
    public static void ApplyWeapons()
    {
        IReadOnlyList<WeaponJsonWeapon> weapons = WeaponJsonReader.ReadDefault();
        DataImportReport preview = WeaponJsonAssetSync.Preview(weapons);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Weapon JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = WeaponJsonAssetSync.Apply(weapons);
        AssetDatabase.Refresh();
        Debug.Log($"Weapon JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
