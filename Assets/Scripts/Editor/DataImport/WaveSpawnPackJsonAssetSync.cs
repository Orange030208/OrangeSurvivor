#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WaveSpawnPackJsonAssetSync
{
    private const string SpawnPackFolder = GameContentAssetPaths.WaveSpawnPacks;
    private const int MinSpawnCount = 1;

    public static DataImportReport Preview(IReadOnlyList<WaveSpawnPackJsonPack> spawnPacks)
    {
        DataImportReport report = new();
        Dictionary<string, WaveSpawnPackSO> assetsById = LoadSpawnPacksById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < spawnPacks.Count; i++)
        {
            WaveSpawnPackJsonPack spawnPack = spawnPacks[i];
            ValidateSpawnPack(spawnPack);
            if (!jsonIds.Add(spawnPack.packId))
            {
                report.AddBlocker($"Duplicated spawn pack id in JSON: {spawnPack.packId}");
                continue;
            }

            if (assetsById.TryGetValue(spawnPack.packId, out WaveSpawnPackSO asset))
            {
                report.AddUpdated($"{spawnPack.packId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{spawnPack.packId} -> {BuildSpawnPackPath(spawnPack.packId)}");
            }
        }

        foreach (KeyValuePair<string, WaveSpawnPackSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddDeleteCandidate($"{pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<WaveSpawnPackJsonPack> spawnPacks)
    {
        DataImportReport report = Preview(spawnPacks);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(SpawnPackFolder);
        Dictionary<string, WaveSpawnPackSO> assetsById = LoadSpawnPacksById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);
        for (int i = 0; i < spawnPacks.Count; i++)
        {
            WaveSpawnPackJsonPack packData = spawnPacks[i];
            jsonIds.Add(packData.packId);
            if (!assetsById.TryGetValue(packData.packId, out WaveSpawnPackSO pack))
            {
                pack = ScriptableObject.CreateInstance<WaveSpawnPackSO>();
                pack.name = DataImportAssetUtility.ToSafeAssetFileName(packData.packId);
                AssetDatabase.CreateAsset(pack, BuildSpawnPackPath(packData.packId));
                assetsById[packData.packId] = pack;
            }

            ApplySpawnPack(pack, packData);
        }

        foreach (KeyValuePair<string, WaveSpawnPackSO> pair in assetsById)
        {
            if (jsonIds.Contains(pair.Key))
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(pair.Value);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                report.AddBlocker($"Cannot delete stale wave spawn pack '{pair.Key}' because its asset path is missing.");
                continue;
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                report.AddBlocker($"Failed to delete stale wave spawn pack asset: {assetPath}");
            }
        }

        AssetDatabase.SaveAssets();
        return report;
    }

    private static void ValidateSpawnPack(WaveSpawnPackJsonPack spawnPack)
    {
        if (spawnPack.entries.Count == 0)
        {
            throw new DataImportException($"{spawnPack.packId} must contain at least one entry.");
        }

        for (int i = 0; i < spawnPack.entries.Count; i++)
        {
            CreateEntry(spawnPack.packId, i, spawnPack.entries[i]);
        }
    }

    private static void ApplySpawnPack(WaveSpawnPackSO pack, WaveSpawnPackJsonPack data)
    {
        List<WaveSpawnPackEntry> entries = new(data.entries.Count);
        for (int i = 0; i < data.entries.Count; i++)
        {
            entries.Add(CreateEntry(data.packId, i, data.entries[i]));
        }

        pack.InitializeRuntime(data.packId, entries);
        EditorUtility.SetDirty(pack);
    }

    private static WaveSpawnPackEntry CreateEntry(string packId, int index, WaveSpawnPackJsonEntry data)
    {
        if (data.spawnCount < MinSpawnCount)
        {
            throw new DataImportException($"{packId} entries[{index}] spawnCount must be >= {MinSpawnCount}.");
        }

        EnemySO enemy = AssetDatabase.LoadAssetAtPath<EnemySO>(data.enemyAssetPath);
        if (enemy == null)
        {
            throw new DataImportException($"{packId} entries[{index}] cannot load EnemySO at '{data.enemyAssetPath}'.");
        }

        return new WaveSpawnPackEntry(
            enemy,
            data.spawnCount,
            ResolveTags(packId, index, data.enemyTags),
            data.overrideTags);
    }

    private static WaveEnemyTag ResolveTags(string packId, int entryIndex, IReadOnlyList<string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return WaveEnemyTag.Normal;
        }

        WaveEnemyTag result = WaveEnemyTag.None;
        for (int i = 0; i < tags.Count; i++)
        {
            string value = tags[i];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result |= ParseEnum<WaveEnemyTag>(value, packId, $"entries[{entryIndex}].enemyTags[{i}]");
        }

        return result == WaveEnemyTag.None ? WaveEnemyTag.Normal : result;
    }

    private static TEnum ParseEnum<TEnum>(string value, string packId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{packId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, WaveSpawnPackSO> LoadSpawnPacksById()
    {
        Dictionary<string, WaveSpawnPackSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<WaveSpawnPackSO> assets = DataImportAssetUtility.LoadAssets<WaveSpawnPackSO>(SpawnPackFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            WaveSpawnPackSO pack = assets[i];
            if (pack == null || string.IsNullOrWhiteSpace(pack.PackId))
            {
                continue;
            }

            result[pack.PackId] = pack;
        }

        return result;
    }

    private static string BuildSpawnPackPath(string packId)
    {
        return $"{SpawnPackFolder}/{DataImportAssetUtility.ToSafeAssetFileName(packId)}.asset";
    }
}
#endif
