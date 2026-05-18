#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WaveSpawnPackJsonReader
{
    public const string DefaultJsonPath = GameContentAssetPaths.WaveSpawnPacks + "/spawn_packs.json";

    public static IReadOnlyList<WaveSpawnPackJsonPack> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<WaveSpawnPackJsonPack> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Wave spawn pack JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Wave spawn pack JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            WaveSpawnPackJsonFile file = JsonUtility.FromJson<WaveSpawnPackJsonFile>(WrapRootArray(json));
            if (file?.spawnPacks == null)
            {
                throw new DataImportException($"Wave spawn pack JSON has no spawnPacks array: {absolutePath}");
            }

            Validate(file.spawnPacks, absolutePath);
            return file.spawnPacks;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read wave spawn pack JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"spawnPacks\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<WaveSpawnPackJsonPack> spawnPacks, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < spawnPacks.Count; i++)
        {
            WaveSpawnPackJsonPack spawnPack = spawnPacks[i];
            string packContext = $"{context} spawnPacks[{i}]";
            if (spawnPack == null)
            {
                throw new DataImportException($"{packContext} is null.");
            }

            Require(spawnPack.packId, packContext, nameof(spawnPack.packId));
            if (!ids.Add(spawnPack.packId))
            {
                throw new DataImportException($"{packContext} has duplicated packId '{spawnPack.packId}'.");
            }

            if (spawnPack.entries == null)
            {
                throw new DataImportException($"{spawnPack.packId} has null entries.");
            }

            for (int entryIndex = 0; entryIndex < spawnPack.entries.Count; entryIndex++)
            {
                WaveSpawnPackJsonEntry entry = spawnPack.entries[entryIndex];
                string entryContext = $"{spawnPack.packId} entries[{entryIndex}]";
                if (entry == null)
                {
                    throw new DataImportException($"{entryContext} is null.");
                }

                Require(entry.enemyAssetPath, entryContext, nameof(entry.enemyAssetPath));
                if (entry.enemyTags == null)
                {
                    throw new DataImportException($"{entryContext} has null enemyTags.");
                }
            }
        }
    }

    private static void Require(string value, string context, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DataImportException($"{context} is missing required field '{fieldName}'.");
        }
    }
}
#endif
