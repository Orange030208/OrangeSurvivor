#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WeaponJsonAssetSync
{
    private const string WeaponFolder = GameContentAssetPaths.WeaponsData;
    private const string WeaponRewardPoolPath = GameContentAssetPaths.WeaponRewardPool;

    public static DataImportReport Preview(IReadOnlyList<WeaponJsonWeapon> weapons)
    {
        DataImportReport report = new();
        Dictionary<string, WeaponDataSO> assetsById = LoadWeaponsById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponJsonWeapon weapon = weapons[i];
            ValidateWeapon(weapon);
            if (!jsonIds.Add(weapon.weaponId))
            {
                report.AddBlocker($"Duplicated weaponId in JSON: {weapon.weaponId}");
                continue;
            }

            if (assetsById.TryGetValue(weapon.weaponId, out WeaponDataSO asset))
            {
                report.AddUpdated($"{weapon.weaponId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{weapon.weaponId} -> {BuildWeaponPath(weapon.weaponId)}");
                report.AddWarning($"{weapon.weaponId} is new; icon, attack sequence, projectile, VFX, and SFX references must be assigned outside JSON.");
            }
        }

        foreach (KeyValuePair<string, WeaponDataSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddWarning($"Weapon asset is not represented in JSON and will be kept unchanged: {pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<WeaponJsonWeapon> weapons)
    {
        DataImportReport report = Preview(weapons);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(WeaponFolder);
        Dictionary<string, WeaponDataSO> assetsById = LoadWeaponsById();
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponJsonWeapon weaponData = weapons[i];
            if (!assetsById.TryGetValue(weaponData.weaponId, out WeaponDataSO weapon))
            {
                weapon = ScriptableObject.CreateInstance<WeaponDataSO>();
                weapon.name = weaponData.weaponId;
                AssetDatabase.CreateAsset(weapon, BuildWeaponPath(weaponData.weaponId));
                assetsById[weaponData.weaponId] = weapon;
            }

            ApplyWeapon(weapon, weaponData);
        }

        GameContentCatalogBuildUtility.RebuildRuntimeContentCatalog();
        AssetDatabase.SaveAssets();
        return report;
    }

    public static void RebuildWeaponRewardPool(IReadOnlyList<WeaponJsonWeapon> weaponRows = null)
    {
        DataImportAssetUtility.EnsureFolder(GameContentAssetPaths.CatalogPools);
        List<ContentPoolEntry> entries = new();
        Dictionary<string, WeaponJsonWeapon> rowsById = BuildRowsById(weaponRows);
        IReadOnlyList<WeaponDataSO> weapons = DataImportAssetUtility.LoadAssets<WeaponDataSO>(WeaponFolder);
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponDataSO weapon = weapons[i];
            WeaponJsonWeapon row = ResolveWeaponRow(rowsById, weapon);
            ContentPoolEntry entry = WeaponContentPoolTuningUtility.CreateRewardEntry(
                weapon,
                ResolveBaseWeight(row),
                ResolveOpenWave(row),
                ResolveCloseWave(row));
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(WeaponRewardPoolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<ContentPoolSO>();
            AssetDatabase.CreateAsset(pool, WeaponRewardPoolPath);
        }

        pool.Initialize(entries, 1, false);
        EditorUtility.SetDirty(pool);
    }

    private static void ValidateWeapon(WeaponJsonWeapon weapon)
    {
        ParseEnum<WeaponAttackTimingMode>(weapon.attackTimingMode, weapon.weaponId, nameof(weapon.attackTimingMode));
        ParseEnum<WeaponTargetingMode>(weapon.targetingMode, weapon.weaponId, nameof(weapon.targetingMode));
        ResolveTags(weapon);

        if (weapon.openWave < 1)
        {
            throw new DataImportException($"{weapon.weaponId} openWave must be >= 1.");
        }

        if (weapon.closeWave < 0)
        {
            throw new DataImportException($"{weapon.weaponId} closeWave must be >= 0.");
        }

        if (weapon.closeWave > 0 && weapon.closeWave < weapon.openWave)
        {
            throw new DataImportException($"{weapon.weaponId} closeWave must be 0 or >= openWave.");
        }

        for (int i = 0; i < weapon.levelStats.Count; i++)
        {
            WeaponJsonLevelStat stat = weapon.levelStats[i];
            CreateBenefit(stat.statBenefits);
            for (int modifierIndex = 0; modifierIndex < stat.holderModifiers.Count; modifierIndex++)
            {
                CreateModifier(weapon.weaponId, i, modifierIndex, stat.holderModifiers[modifierIndex]);
            }
        }
    }

    private static void ApplyWeapon(WeaponDataSO weapon, WeaponJsonWeapon data)
    {
        SerializedObject serializedObject = new(weapon);
        DataImportAssetUtility.SetString(serializedObject, "weaponId", data.weaponId);
        DataImportAssetUtility.SetString(serializedObject, "itemName", data.itemName);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "itemPrice").intValue = Mathf.Max(0, data.itemPrice);
        DataImportAssetUtility.SetEnum(serializedObject, "itemType", ItemType.Weapon);
        DataImportAssetUtility.SetString(serializedObject, "itemDescription", data.itemDescription);
        WriteTags(serializedObject, data);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "visualForwardAngle").floatValue = data.visualForwardAngle;
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "holdAimWhenAttackReady").boolValue = data.holdAimWhenAttackReady;
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "attackSequenceOccupancy").floatValue = Mathf.Clamp(data.attackSequenceOccupancy, 0.1f, 1f);
        DataImportAssetUtility.SetEnum(serializedObject, "attackTimingMode", ParseEnum<WeaponAttackTimingMode>(data.attackTimingMode, data.weaponId, nameof(data.attackTimingMode)));
        DataImportAssetUtility.SetEnum(serializedObject, "targetingMode", ParseEnum<WeaponTargetingMode>(data.targetingMode, data.weaponId, nameof(data.targetingMode)));
        WriteSpawnPoints(serializedObject, data);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "enableHitBox").boolValue = data.enableHitBox;
        WriteVector2(DataImportAssetUtility.FindRequiredProperty(serializedObject, "hitBoxSize"), data.hitBoxSize, 1f, 1f, 0.01f);
        WriteVector2(DataImportAssetUtility.FindRequiredProperty(serializedObject, "hitBoxOffset"), data.hitBoxOffset, 0f, 0f, float.NegativeInfinity);
        WriteLevelStats(serializedObject, data);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void WriteTags(SerializedObject serializedObject, WeaponJsonWeapon weapon)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "tags");
        List<WeaponTag> tags = ResolveTags(weapon);
        property.arraySize = tags.Count;
        for (int i = 0; i < tags.Count; i++)
        {
            property.GetArrayElementAtIndex(i).intValue = (int)tags[i];
        }
    }

    private static void WriteSpawnPoints(SerializedObject serializedObject, WeaponJsonWeapon weapon)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "spawnPoints");
        property.arraySize = weapon.spawnPoints.Count;
        for (int i = 0; i < weapon.spawnPoints.Count; i++)
        {
            WeaponJsonSpawnPoint spawnPoint = weapon.spawnPoints[i];
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            DataImportAssetUtility.FindRequiredProperty(element, "id").stringValue = spawnPoint.id ?? string.Empty;
            WriteVector2(DataImportAssetUtility.FindRequiredProperty(element, "localPosition"), spawnPoint.localPosition, 0f, 0f, float.NegativeInfinity);
            DataImportAssetUtility.FindRequiredProperty(element, "localRotationOffset").floatValue = spawnPoint.localRotationOffset;
        }
    }

    private static void WriteLevelStats(SerializedObject serializedObject, WeaponJsonWeapon weapon)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "levelStats");
        property.arraySize = weapon.levelStats.Count;
        for (int i = 0; i < weapon.levelStats.Count; i++)
        {
            WeaponJsonLevelStat stat = weapon.levelStats[i];
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            DataImportAssetUtility.FindRequiredProperty(element, "level").intValue = stat.level;
            DataImportAssetUtility.FindRequiredProperty(element, "attack").floatValue = Mathf.Max(0f, stat.attack);
            DataImportAssetUtility.FindRequiredProperty(element, "attackSpeed").floatValue = PropValueUtility.ClampEffectiveAttackSpeedPoints(stat.attackSpeed);
            DataImportAssetUtility.FindRequiredProperty(element, "criticalChance").floatValue = Mathf.Clamp(stat.criticalChance, 0f, 100f);
            DataImportAssetUtility.FindRequiredProperty(element, "criticalPercent").floatValue = Mathf.Max(100f, stat.criticalPercent);
            DataImportAssetUtility.FindRequiredProperty(element, "range").floatValue = Mathf.Max(0f, stat.range);
            DataImportAssetUtility.FindRequiredProperty(element, "knockbackStrength").floatValue = Mathf.Max(0f, stat.knockbackStrength);
            WriteBenefit(DataImportAssetUtility.FindRequiredProperty(element, "statBenefits"), stat.statBenefits);
            WriteHolderModifiers(element, weapon.weaponId, i, stat);
        }
    }

    private static void WriteHolderModifiers(
        SerializedProperty levelStatProperty,
        string weaponId,
        int levelStatIndex,
        WeaponJsonLevelStat stat)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(levelStatProperty, "holderModifiers");
        property.arraySize = stat.holderModifiers.Count;
        for (int i = 0; i < stat.holderModifiers.Count; i++)
        {
            PropModifierData modifier = CreateModifier(weaponId, levelStatIndex, i, stat.holderModifiers[i]);
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            DataImportAssetUtility.FindRequiredProperty(element, "propType").intValue = (int)modifier.propType;
            DataImportAssetUtility.FindRequiredProperty(element, "modifierType").intValue = (int)modifier.modifierType;
            DataImportAssetUtility.FindRequiredProperty(element, "value").floatValue = modifier.value;
        }
    }

    private static void WriteBenefit(SerializedProperty property, WeaponJsonBenefit benefit)
    {
        WeaponBenefitData value = CreateBenefit(benefit);
        DataImportAssetUtility.FindRequiredProperty(property, "attackSpeedBenefitPercent").floatValue = value.AttackSpeedBenefitPercent;
        DataImportAssetUtility.FindRequiredProperty(property, "criticalChanceBenefitPercent").floatValue = value.CriticalChanceBenefitPercent;
        DataImportAssetUtility.FindRequiredProperty(property, "criticalPercentBenefitPercent").floatValue = value.CriticalPercentBenefitPercent;
        DataImportAssetUtility.FindRequiredProperty(property, "rangeBenefitPercent").floatValue = value.RangeBenefitPercent;
        DataImportAssetUtility.FindRequiredProperty(property, "knockbackStrengthBenefitPercent").floatValue = value.KnockbackStrengthBenefitPercent;
        DataImportAssetUtility.FindRequiredProperty(property, "meleeAttackUsagePercent").floatValue = value.MeleeAttackUsagePercent;
        DataImportAssetUtility.FindRequiredProperty(property, "rangedAttackUsagePercent").floatValue = value.RangedAttackUsagePercent;
        DataImportAssetUtility.FindRequiredProperty(property, "magicAttackUsagePercent").floatValue = value.MagicAttackUsagePercent;
        DataImportAssetUtility.FindRequiredProperty(property, "summonAttackUsagePercent").floatValue = value.SummonAttackUsagePercent;
    }

    private static void WriteVector2(
        SerializedProperty property,
        WeaponJsonVector2 value,
        float fallbackX,
        float fallbackY,
        float min)
    {
        float x = value != null ? value.x : fallbackX;
        float y = value != null ? value.y : fallbackY;
        property.vector2Value = new Vector2(Mathf.Max(min, x), Mathf.Max(min, y));
    }

    private static WeaponBenefitData CreateBenefit(WeaponJsonBenefit data)
    {
        if (data == null)
        {
            throw new DataImportException("Weapon benefit data is missing.");
        }

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

    private static PropModifierData CreateModifier(
        string weaponId,
        int levelStatIndex,
        int modifierIndex,
        WeaponJsonPropModifier data)
    {
        if (data == null)
        {
            throw new DataImportException($"{weaponId} levelStats[{levelStatIndex}] holderModifiers[{modifierIndex}] is null.");
        }

        PropType propType = ParseEnum<PropType>(data.propType, weaponId, $"levelStats[{levelStatIndex}].holderModifiers[{modifierIndex}].propType");
        PropModifierType modifierType = ParseEnum<PropModifierType>(data.modifierType, weaponId, $"levelStats[{levelStatIndex}].holderModifiers[{modifierIndex}].modifierType");
        return new PropModifierData(propType, modifierType, data.value);
    }

    private static List<WeaponTag> ResolveTags(WeaponJsonWeapon weapon)
    {
        List<WeaponTag> result = new();
        for (int i = 0; i < weapon.tags.Count; i++)
        {
            string value = weapon.tags[i];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            WeaponTag tag = ParseEnum<WeaponTag>(value, weapon.weaponId, $"tags[{i}]");
            if (!result.Contains(tag))
            {
                result.Add(tag);
            }
        }

        return result;
    }

    private static TEnum ParseEnum<TEnum>(string value, string weaponId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{weaponId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, WeaponDataSO> LoadWeaponsById()
    {
        Dictionary<string, WeaponDataSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<WeaponDataSO> assets = DataImportAssetUtility.LoadAssets<WeaponDataSO>(WeaponFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            WeaponDataSO weapon = assets[i];
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.WeaponId))
            {
                continue;
            }

            result[weapon.WeaponId] = weapon;
        }

        return result;
    }

    private static Dictionary<string, WeaponJsonWeapon> BuildRowsById(IReadOnlyList<WeaponJsonWeapon> rows)
    {
        Dictionary<string, WeaponJsonWeapon> result = new(StringComparer.Ordinal);
        if (rows == null)
        {
            return result;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            WeaponJsonWeapon row = rows[i];
            if (row == null || string.IsNullOrWhiteSpace(row.weaponId))
            {
                continue;
            }

            result[row.weaponId] = row;
        }

        return result;
    }

    private static WeaponJsonWeapon ResolveWeaponRow(
        IReadOnlyDictionary<string, WeaponJsonWeapon> rowsById,
        WeaponDataSO weapon)
    {
        if (weapon == null || rowsById == null)
        {
            return null;
        }

        return rowsById.TryGetValue(weapon.WeaponId, out WeaponJsonWeapon row)
            ? row
            : null;
    }

    private static float ResolveBaseWeight(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(0f, row.baseWeight)
            : WeaponContentPoolTuningUtility.DefaultRewardWeaponWeight;
    }

    private static int ResolveOpenWave(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(WeaponContentPoolTuningUtility.DefaultOpenWave, row.openWave)
            : WeaponContentPoolTuningUtility.DefaultOpenWave;
    }

    private static int ResolveCloseWave(WeaponJsonWeapon row)
    {
        return row != null
            ? Mathf.Max(WeaponContentPoolTuningUtility.DefaultCloseWave, row.closeWave)
            : WeaponContentPoolTuningUtility.DefaultCloseWave;
    }

    private static string BuildWeaponPath(string weaponId)
    {
        return $"{WeaponFolder}/{DataImportAssetUtility.ToSafeAssetFileName(weaponId)}.asset";
    }
}
#endif
