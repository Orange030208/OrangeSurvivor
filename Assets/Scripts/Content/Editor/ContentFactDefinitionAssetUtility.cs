#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ContentFactDefinitionAssetUtility
{
    private const string FactFolder = "Assets/ScriptableObjects/Content/Facts";

    [MenuItem("Survivors/Content/Create Built-in Fact Definitions")]
    public static void CreateBuiltInFactDefinitions()
    {
        EnsureFolder(FactFolder);

        CreateOrUpdateFact("Current Wave.asset", ContentFactIds.CurrentWave, "Current Wave", FactValueType.Int, FactDefinitionBuiltInKind.CurrentWave);
        CreateOrUpdateFact("Luck.asset", ContentFactIds.Luck, "Luck", FactValueType.Float, FactDefinitionBuiltInKind.Luck);
        CreateOrUpdateFact("Shop Refresh Count.asset", ContentFactIds.ShopRefreshCount, "Shop Refresh Count", FactValueType.Int, FactDefinitionBuiltInKind.ShopRefreshCount);
        CreateOrUpdateFact("Shop Reroll Count.asset", ContentFactIds.ShopRerollCount, "Shop Reroll Count", FactValueType.Int, FactDefinitionBuiltInKind.ShopRerollCount);
        CreateOrUpdateFact("Character.asset", ContentFactIds.Character, "Character", FactValueType.UnityObject, FactDefinitionBuiltInKind.Character);
        CreateOrUpdateFact("Owned Weapon Count.asset", ContentFactIds.OwnedWeaponCount, "Owned Weapon Count", FactValueType.Int, FactDefinitionBuiltInKind.OwnedWeaponCount);
        CreateOrUpdateFact("Wave Id.asset", ContentFactIds.WaveId, "Wave Id", FactValueType.String, FactDefinitionBuiltInKind.WaveId);
        CreateOrUpdateFact("Wave Track Id.asset", ContentFactIds.WaveTrackId, "Wave Track Id", FactValueType.String, FactDefinitionBuiltInKind.WaveTrackId);
        CreateOrUpdateFact("Wave Progress Percent.asset", ContentFactIds.WaveProgressPercent, "Wave Progress Percent", FactValueType.Float, FactDefinitionBuiltInKind.WaveProgressPercent);

        foreach (UpgradeCardTag tag in Enum.GetValues(typeof(UpgradeCardTag)))
        {
            CreateOrUpdateFact(
                $"Upgrade Card Tag Pick Count {tag}.asset",
                $"upgrade_card_tag_pick_count.{tag}",
                $"Upgrade Card Tag Pick Count/{tag}",
                FactValueType.Int,
                FactDefinitionBuiltInKind.UpgradeCardTagPickCount,
                upgradeCardTag: tag);
        }

        foreach (WeaponTag tag in Enum.GetValues(typeof(WeaponTag)))
        {
            CreateOrUpdateFact(
                $"Owned Weapon Tag Count {tag}.asset",
                $"owned_weapon_tag_count.{tag}",
                $"Owned Weapon Tag Count/{tag}",
                FactValueType.Int,
                FactDefinitionBuiltInKind.OwnedWeaponTagCount,
                weaponTag: tag);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created or updated built-in content fact definitions in {FactFolder}.");
    }

    private static void CreateOrUpdateFact(
        string fileName,
        string factId,
        string displayName,
        FactValueType valueType,
        FactDefinitionBuiltInKind builtInKind,
        PropType propType = default,
        UpgradeCardTag upgradeCardTag = default,
        WeaponTag weaponTag = default)
    {
        string assetPath = $"{FactFolder}/{fileName}";
        FactDefinitionSO fact = AssetDatabase.LoadAssetAtPath<FactDefinitionSO>(assetPath);
        if (fact == null)
        {
            fact = ScriptableObject.CreateInstance<FactDefinitionSO>();
            AssetDatabase.CreateAsset(fact, assetPath);
        }

        SerializedObject serializedObject = new(fact);
        serializedObject.FindProperty("factId").stringValue = factId;
        serializedObject.FindProperty("displayName").stringValue = displayName;
        serializedObject.FindProperty("valueType").enumValueIndex = (int)valueType;
        serializedObject.FindProperty("builtInKind").enumValueIndex = (int)builtInKind;
        serializedObject.FindProperty("propType").enumValueIndex = (int)propType;
        serializedObject.FindProperty("upgradeCardTag").enumValueIndex = (int)upgradeCardTag;
        serializedObject.FindProperty("weaponTag").enumValueIndex = (int)weaponTag;
        serializedObject.FindProperty("weaponData").objectReferenceValue = null;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fact);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
