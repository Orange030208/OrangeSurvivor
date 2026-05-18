#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuffJsonAssetSync
{
    private const string BuffFolder = GameContentAssetPaths.CombatBuffs;
    private const float MinDurationSeconds = 0.01f;
    private const float MinTickIntervalSeconds = 0.01f;
    private const int MinStackCount = 1;

    public static DataImportReport Preview(IReadOnlyList<BuffJsonBuff> buffs)
    {
        DataImportReport report = new();
        Dictionary<string, BuffDataSO> assetsById = LoadBuffsById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < buffs.Count; i++)
        {
            BuffJsonBuff buff = buffs[i];
            ValidateBuff(buff);
            if (!jsonIds.Add(buff.buffId))
            {
                report.AddBlocker($"Duplicated buffId in JSON: {buff.buffId}");
                continue;
            }

            if (assetsById.TryGetValue(buff.buffId, out BuffDataSO asset))
            {
                report.AddUpdated($"{buff.buffId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{buff.buffId} -> {BuildBuffPath(buff.buffId)}");
            }
        }

        foreach (KeyValuePair<string, BuffDataSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddDeleteCandidate($"{pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<BuffJsonBuff> buffs)
    {
        DataImportReport report = Preview(buffs);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(BuffFolder);
        Dictionary<string, BuffDataSO> assetsById = LoadBuffsById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffJsonBuff buffData = buffs[i];
            jsonIds.Add(buffData.buffId);
            if (!assetsById.TryGetValue(buffData.buffId, out BuffDataSO buff))
            {
                buff = ScriptableObject.CreateInstance<BuffDataSO>();
                buff.name = buffData.buffId;
                AssetDatabase.CreateAsset(buff, BuildBuffPath(buffData.buffId));
                assetsById[buffData.buffId] = buff;
            }

            ApplyBuff(buff, buffData);
        }

        foreach (KeyValuePair<string, BuffDataSO> pair in assetsById)
        {
            if (jsonIds.Contains(pair.Key))
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(pair.Value);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                report.AddBlocker($"Cannot delete stale buff '{pair.Key}' because its asset path is missing.");
                continue;
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                report.AddBlocker($"Failed to delete stale buff asset: {assetPath}");
            }
        }

        AssetDatabase.SaveAssets();
        return report;
    }

    private static void ValidateBuff(BuffJsonBuff buff)
    {
        ParseEnum<BuffPolarity>(buff.polarity, buff.buffId, nameof(buff.polarity));
        BuffDurationPolicy durationPolicy =
            ParseEnum<BuffDurationPolicy>(buff.durationPolicy, buff.buffId, nameof(buff.durationPolicy));
        ParseEnum<BuffRefreshMode>(buff.refreshMode, buff.buffId, nameof(buff.refreshMode));
        ParseEnum<BuffOverflowMode>(buff.overflowMode, buff.buffId, nameof(buff.overflowMode));

        if (durationPolicy == BuffDurationPolicy.Timed && buff.durationSeconds < MinDurationSeconds)
        {
            throw new DataImportException($"{buff.buffId} durationSeconds must be >= {MinDurationSeconds} for timed buffs.");
        }

        if (buff.maxStackCount < MinStackCount)
        {
            throw new DataImportException($"{buff.buffId} maxStackCount must be >= {MinStackCount}.");
        }

        for (int i = 0; i < buff.specialFeatures.Count; i++)
        {
            CreateFeature(buff.buffId, i, buff.specialFeatures[i]);
        }
    }

    private static void ApplyBuff(BuffDataSO buff, BuffJsonBuff data)
    {
        SerializedObject serializedObject = new(buff);
        DataImportAssetUtility.SetString(serializedObject, "buffId", data.buffId);
        DataImportAssetUtility.SetString(serializedObject, "displayName", data.displayName);
        DataImportAssetUtility.SetString(serializedObject, "description", data.description);
        DataImportAssetUtility.SetEnum(
            serializedObject,
            "polarity",
            ParseEnum<BuffPolarity>(data.polarity, data.buffId, nameof(data.polarity)));
        DataImportAssetUtility.SetEnum(
            serializedObject,
            "durationPolicy",
            ParseEnum<BuffDurationPolicy>(data.durationPolicy, data.buffId, nameof(data.durationPolicy)));
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "durationSeconds").floatValue =
            Mathf.Max(MinDurationSeconds, data.durationSeconds);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "maxStackCount").intValue =
            Mathf.Max(MinStackCount, data.maxStackCount);
        DataImportAssetUtility.SetEnum(
            serializedObject,
            "refreshMode",
            ParseEnum<BuffRefreshMode>(data.refreshMode, data.buffId, nameof(data.refreshMode)));
        DataImportAssetUtility.SetEnum(
            serializedObject,
            "overflowMode",
            ParseEnum<BuffOverflowMode>(data.overflowMode, data.buffId, nameof(data.overflowMode)));
        WriteSpecialFeatures(serializedObject, data);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(buff);
    }

    private static void WriteSpecialFeatures(SerializedObject serializedObject, BuffJsonBuff buff)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "specialFeatures");
        property.arraySize = buff.specialFeatures.Count;
        for (int i = 0; i < buff.specialFeatures.Count; i++)
        {
            property.GetArrayElementAtIndex(i).managedReferenceValue = CreateFeature(buff.buffId, i, buff.specialFeatures[i]);
        }
    }

    private static FeatureBase CreateFeature(string buffId, int index, BuffJsonFeature data)
    {
        return data.type switch
        {
            nameof(PropertyModifierFeature) => CreatePropertyModifierFeature(buffId, index, data),
            nameof(DamageOverTimeFeature) => CreateDamageOverTimeFeature(buffId, index, data),
            _ => throw new DataImportException($"{buffId} specialFeatures[{index}] has unsupported feature type '{data.type}'.")
        };
    }

    private static PropertyModifierFeature CreatePropertyModifierFeature(string buffId, int index, BuffJsonFeature data)
    {
        if (data.modifier == null)
        {
            throw new DataImportException($"{buffId} specialFeatures[{index}] is missing modifier.");
        }

        PropType propType = ParseEnum<PropType>(data.modifier.propType, buffId, "modifier.propType");
        PropModifierType modifierType =
            ParseEnum<PropModifierType>(data.modifier.modifierType, buffId, "modifier.modifierType");
        return new PropertyModifierFeature(new PropModifierData(propType, modifierType, data.modifier.value));
    }

    private static DamageOverTimeFeature CreateDamageOverTimeFeature(string buffId, int index, BuffJsonFeature data)
    {
        if (data.damageOverTime == null)
        {
            throw new DataImportException($"{buffId} specialFeatures[{index}] is missing damageOverTime.");
        }

        if (data.damageOverTime.damagePerSecond <= 0f)
        {
            throw new DataImportException($"{buffId} specialFeatures[{index}] damagePerSecond must be > 0.");
        }

        if (data.damageOverTime.tickIntervalSeconds < MinTickIntervalSeconds)
        {
            throw new DataImportException(
                $"{buffId} specialFeatures[{index}] tickIntervalSeconds must be >= {MinTickIntervalSeconds}.");
        }

        return new DamageOverTimeFeature(
            data.damageOverTime.damagePerSecond,
            data.damageOverTime.tickIntervalSeconds);
    }

    private static TEnum ParseEnum<TEnum>(string value, string buffId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{buffId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, BuffDataSO> LoadBuffsById()
    {
        Dictionary<string, BuffDataSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<BuffDataSO> assets = DataImportAssetUtility.LoadAssets<BuffDataSO>(BuffFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            BuffDataSO buff = assets[i];
            if (buff == null || string.IsNullOrWhiteSpace(buff.BuffId))
            {
                continue;
            }

            result[buff.BuffId] = buff;
        }

        return result;
    }

    private static string BuildBuffPath(string buffId)
    {
        return $"{BuffFolder}/{DataImportAssetUtility.ToSafeAssetFileName(buffId)}.asset";
    }
}
#endif
