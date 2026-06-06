#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AccessoryJsonAssetSync
{
    private const string AccessoryFolder = GameContentAssetPaths.AccessoriesData;

    public static DataImportReport Preview(IReadOnlyList<AccessoryJsonAccessory> accessories)
    {
        DataImportReport report = new();
        Dictionary<string, AccessoryDataSO> assetsById = LoadAccessoriesById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < accessories.Count; i++)
        {
            AccessoryJsonAccessory accessory = accessories[i];
            ValidateAccessory(accessory);
            if (!jsonIds.Add(accessory.accessoryId))
            {
                report.AddBlocker($"Duplicated accessoryId in JSON: {accessory.accessoryId}");
                continue;
            }

            if (assetsById.TryGetValue(accessory.accessoryId, out AccessoryDataSO asset))
            {
                report.AddUpdated($"{accessory.accessoryId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{accessory.accessoryId} -> {BuildAccessoryPath(accessory.accessoryId)}");
                report.AddWarning($"{accessory.accessoryId} is new; icon references must be assigned outside JSON.");
            }
        }

        foreach (KeyValuePair<string, AccessoryDataSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddWarning($"Accessory asset is not represented in JSON and will be kept unchanged: {pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<AccessoryJsonAccessory> accessories)
    {
        DataImportReport report = Preview(accessories);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(AccessoryFolder);
        Dictionary<string, AccessoryDataSO> assetsById = LoadAccessoriesById();
        for (int i = 0; i < accessories.Count; i++)
        {
            AccessoryJsonAccessory accessoryData = accessories[i];
            if (!assetsById.TryGetValue(accessoryData.accessoryId, out AccessoryDataSO accessory))
            {
                accessory = ScriptableObject.CreateInstance<AccessoryDataSO>();
                accessory.name = accessoryData.accessoryId;
                AssetDatabase.CreateAsset(accessory, BuildAccessoryPath(accessoryData.accessoryId));
                assetsById[accessoryData.accessoryId] = accessory;
            }

            ApplyAccessory(accessory, accessoryData);
        }

        RefreshAccessoryDataList();
        AssetDatabase.SaveAssets();
        return report;
    }

    private static void ValidateAccessory(AccessoryJsonAccessory accessory)
    {
        ParseEnum<ContentTier>(accessory.rarity, accessory.accessoryId, nameof(accessory.rarity));
        if (accessory.itemPrice < 0)
        {
            throw new DataImportException($"{accessory.accessoryId} itemPrice must be >= 0.");
        }

        if (accessory.recyclePrice < 0)
        {
            throw new DataImportException($"{accessory.accessoryId} recyclePrice must be >= 0.");
        }

        if (accessory.maxOwnedCount < 0)
        {
            throw new DataImportException($"{accessory.accessoryId} maxOwnedCount must be >= 0.");
        }

        for (int i = 0; i < accessory.propertyModifiers.Count; i++)
        {
            CreateModifier(accessory.accessoryId, $"propertyModifiers[{i}]", accessory.propertyModifiers[i]);
        }

        for (int i = 0; i < accessory.specialFeatures.Count; i++)
        {
            CreateFeature(accessory.accessoryId, i, accessory.specialFeatures[i]);
        }
    }

    private static void ApplyAccessory(AccessoryDataSO accessory, AccessoryJsonAccessory data)
    {
        SerializedObject serializedObject = new(accessory);
        DataImportAssetUtility.SetString(serializedObject, "accessoryId", data.accessoryId);
        DataImportAssetUtility.SetString(serializedObject, "itemName", data.itemName);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "itemPrice").intValue = Mathf.Max(0, data.itemPrice);
        DataImportAssetUtility.SetEnum(serializedObject, "itemType", ItemType.Accessory);
        DataImportAssetUtility.SetString(serializedObject, "itemDescription", data.itemDescription);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "recyclePrice").intValue = Mathf.Max(0, data.recyclePrice);
        DataImportAssetUtility.SetEnum(serializedObject, "tier", ParseEnum<ContentTier>(data.rarity, data.accessoryId, nameof(data.rarity)));
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "maxOwnedCount").intValue = Mathf.Max(0, data.maxOwnedCount);
        WritePropertyModifiers(serializedObject, data);
        WriteSpecialFeatures(serializedObject, data);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(accessory);
    }

    private static void WritePropertyModifiers(SerializedObject serializedObject, AccessoryJsonAccessory accessory)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "propertyModifiers");
        property.arraySize = accessory.propertyModifiers.Count;
        for (int i = 0; i < accessory.propertyModifiers.Count; i++)
        {
            PropModifierData modifier = CreateModifier(accessory.accessoryId, $"propertyModifiers[{i}]", accessory.propertyModifiers[i]);
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            DataImportAssetUtility.FindRequiredProperty(element, "propType").intValue = (int)modifier.propType;
            DataImportAssetUtility.FindRequiredProperty(element, "modifierType").intValue = (int)modifier.modifierType;
            DataImportAssetUtility.FindRequiredProperty(element, "value").floatValue = modifier.value;
        }
    }

    private static void WriteSpecialFeatures(SerializedObject serializedObject, AccessoryJsonAccessory accessory)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "specialFeatures");
        property.arraySize = accessory.specialFeatures.Count;
        for (int i = 0; i < accessory.specialFeatures.Count; i++)
        {
            property.GetArrayElementAtIndex(i).managedReferenceValue =
                CreateFeature(accessory.accessoryId, i, accessory.specialFeatures[i]);
        }
    }

    private static FeatureBase CreateFeature(string accessoryId, int index, AccessoryJsonFeature data)
    {
        return data.type switch
        {
            nameof(PropertyModifierFeature) => CreatePropertyModifierFeature(accessoryId, index, data),
            nameof(WeaponBenefitBonusModifierFeature) => CreateWeaponBenefitBonusModifierFeature(accessoryId, index, data),
            _ => throw new DataImportException($"{accessoryId} specialFeatures[{index}] has unsupported feature type '{data.type}'.")
        };
    }

    private static PropertyModifierFeature CreatePropertyModifierFeature(string accessoryId, int index, AccessoryJsonFeature data)
    {
        if (data.modifier == null)
        {
            throw new DataImportException($"{accessoryId} specialFeatures[{index}] is missing modifier.");
        }

        return new PropertyModifierFeature(CreateModifier(accessoryId, $"specialFeatures[{index}].modifier", data.modifier));
    }

    private static WeaponBenefitBonusModifierFeature CreateWeaponBenefitBonusModifierFeature(
        string accessoryId,
        int index,
        AccessoryJsonFeature data)
    {
        if (data.benefitBonus == null)
        {
            throw new DataImportException($"{accessoryId} specialFeatures[{index}] is missing benefitBonus.");
        }

        return new WeaponBenefitBonusModifierFeature(CreateBenefit(data.benefitBonus));
    }

    private static PropModifierData CreateModifier(string accessoryId, string fieldPath, AccessoryJsonPropModifier data)
    {
        if (data == null)
        {
            throw new DataImportException($"{accessoryId} {fieldPath} is null.");
        }

        PropType propType = ParseEnum<PropType>(data.propType, accessoryId, $"{fieldPath}.propType");
        PropModifierType modifierType = ParseEnum<PropModifierType>(data.modifierType, accessoryId, $"{fieldPath}.modifierType");
        return new PropModifierData(propType, modifierType, data.value);
    }

    private static WeaponBenefitData CreateBenefit(AccessoryJsonWeaponBenefit data)
    {
        return new WeaponBenefitData(
            data.attackSpeedBenefitPercent,
            data.criticalChanceBenefitPercent,
            data.criticalPercentBenefitPercent,
            data.rangeBenefitPercent,
            data.knockbackStrengthBenefitPercent,
            data.meleeAttackUsagePercent,
            data.rangedAttackUsagePercent,
            data.magicAttackUsagePercent,
            data.summonAttackUsagePercent);
    }

    private static TEnum ParseEnum<TEnum>(string value, string accessoryId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{accessoryId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, AccessoryDataSO> LoadAccessoriesById()
    {
        Dictionary<string, AccessoryDataSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<AccessoryDataSO> assets = DataImportAssetUtility.LoadAssets<AccessoryDataSO>(AccessoryFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            AccessoryDataSO accessory = assets[i];
            if (accessory == null || string.IsNullOrWhiteSpace(accessory.AccessoryId))
            {
                continue;
            }

            result[accessory.AccessoryId] = accessory;
        }

        return result;
    }

    private static void RefreshAccessoryDataList()
    {
        AccessoryDataListSO accessoryDataList =
            AssetDatabase.LoadAssetAtPath<AccessoryDataListSO>(GameContentAssetPaths.AccessoryDataList);
        if (accessoryDataList == null)
        {
            return;
        }

        accessoryDataList.RefreshAccessories();
    }

    private static string BuildAccessoryPath(string accessoryId)
    {
        return $"{AccessoryFolder}/{DataImportAssetUtility.ToSafeAssetFileName(accessoryId)}.asset";
    }
}
#endif
