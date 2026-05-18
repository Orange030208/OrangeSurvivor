#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class WaveJsonAssetSync
{
    private const string WaveFolder = GameContentAssetPaths.WavesData;
    private const float MinDurationSeconds = 1f;

    public static DataImportReport Preview(IReadOnlyList<WaveJsonWave> waves)
    {
        DataImportReport report = new();
        Dictionary<string, WaveDefinitionSO> assetsById = LoadWavesById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < waves.Count; i++)
        {
            WaveJsonWave wave = waves[i];
            ValidateWave(wave);
            if (!jsonIds.Add(wave.waveId))
            {
                report.AddBlocker($"Duplicated waveId in JSON: {wave.waveId}");
                continue;
            }

            if (assetsById.TryGetValue(wave.waveId, out WaveDefinitionSO asset))
            {
                report.AddUpdated($"{wave.waveId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{wave.waveId} -> {BuildWavePath(wave.waveId)}");
            }
        }

        foreach (KeyValuePair<string, WaveDefinitionSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddWarning($"Wave asset is not represented in JSON and will be kept unchanged: {pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<WaveJsonWave> waves)
    {
        DataImportReport report = Preview(waves);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(WaveFolder);
        Dictionary<string, WaveDefinitionSO> assetsById = LoadWavesById();
        for (int i = 0; i < waves.Count; i++)
        {
            WaveJsonWave waveData = waves[i];
            if (!assetsById.TryGetValue(waveData.waveId, out WaveDefinitionSO wave))
            {
                wave = ScriptableObject.CreateInstance<WaveDefinitionSO>();
                wave.name = DataImportAssetUtility.ToSafeAssetFileName(waveData.waveId);
                AssetDatabase.CreateAsset(wave, BuildWavePath(waveData.waveId));
                assetsById[waveData.waveId] = wave;
            }

            ApplyWave(wave, waveData);
        }

        AssetDatabase.SaveAssets();
        return report;
    }

    private static void ValidateWave(WaveJsonWave wave)
    {
        ParseEnum<WaveCompletionMode>(wave.completionMode, wave.waveId, nameof(wave.completionMode));
        if (wave.durationSeconds < MinDurationSeconds)
        {
            throw new DataImportException($"{wave.waveId} durationSeconds must be >= {MinDurationSeconds}.");
        }

        CreateSpawnLocation(wave.waveId, wave.spawnLocation);
    }

    private static void ApplyWave(WaveDefinitionSO wave, WaveJsonWave data)
    {
        SerializedObject serializedObject = new(wave);
        DataImportAssetUtility.SetString(serializedObject, "waveId", data.waveId);
        DataImportAssetUtility.SetString(serializedObject, "displayName", data.displayName);
        DataImportAssetUtility.FindRequiredProperty(serializedObject, "duration").floatValue =
            Mathf.Max(MinDurationSeconds, data.durationSeconds);
        DataImportAssetUtility.SetEnum(
            serializedObject,
            "completionMode",
            ParseEnum<WaveCompletionMode>(data.completionMode, data.waveId, nameof(data.completionMode)));
        WriteSpawnLocation(serializedObject, data.waveId, data.spawnLocation);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wave);
    }

    private static void WriteSpawnLocation(SerializedObject serializedObject, string waveId, WaveJsonSpawnLocation data)
    {
        SpawnLocationDefinition spawnLocation = CreateSpawnLocation(waveId, data);
        SerializedProperty spawnLocationProperty = DataImportAssetUtility.FindRequiredProperty(serializedObject, "spawnLocation");
        SerializedProperty resolverSettingsProperty = DataImportAssetUtility.FindRequiredProperty(spawnLocationProperty, "resolverSettings");
        SerializedProperty strategyProperty = DataImportAssetUtility.FindRequiredProperty(spawnLocationProperty, "strategy");

        WriteResolverSettings(resolverSettingsProperty, spawnLocation.ResolverSettings);
        strategyProperty.managedReferenceValue = spawnLocation.Strategy;
    }

    private static SpawnLocationDefinition CreateSpawnLocation(string waveId, WaveJsonSpawnLocation data)
    {
        if (data == null)
        {
            throw new DataImportException($"{waveId} is missing spawnLocation.");
        }

        if (data.resolverSettings == null)
        {
            throw new DataImportException($"{waveId} is missing spawnLocation.resolverSettings.");
        }

        if (data.strategy == null)
        {
            throw new DataImportException($"{waveId} is missing spawnLocation.strategy.");
        }

        SpawnLocationResolverSettings resolverSettings = new(
            ResolveBoundsPadding(data.resolverSettings),
            ResolveAttempts(data.resolverSettings),
            ResolveSpawnClearance(data.resolverSettings),
            ToVector2(data.resolverSettings.minBounds, new Vector2(-12f, -12f)),
            ToVector2(data.resolverSettings.maxBounds, new Vector2(12f, 12f)),
            ToLayerNames(data.resolverSettings));

        SpawnLocationStrategyModel strategy = data.strategy.type switch
        {
            nameof(AroundPlayerRingSpawnLocationStrategy) => new AroundPlayerRingSpawnLocationStrategy(
                ResolveMinDistance(data.strategy),
                ResolveMaxDistance(data.strategy)),
            nameof(RandomInsideMapSpawnLocationStrategy) => new RandomInsideMapSpawnLocationStrategy(),
            nameof(RandomMapEdgeSpawnLocationStrategy) => new RandomMapEdgeSpawnLocationStrategy(),
            _ => throw new DataImportException($"{waveId} has unsupported spawnLocation.strategy type '{data.strategy.type}'.")
        };

        return new SpawnLocationDefinition(resolverSettings, strategy);
    }

    private static void WriteResolverSettings(SerializedProperty property, SpawnLocationResolverSettings settings)
    {
        DataImportAssetUtility.FindRequiredProperty(property, "boundsPadding").floatValue = settings.BoundsPadding;
        DataImportAssetUtility.FindRequiredProperty(property, "resolveAttempts").intValue = settings.ResolveAttempts;
        DataImportAssetUtility.FindRequiredProperty(property, "spawnClearance").floatValue = settings.SpawnClearance;
        DataImportAssetUtility.FindRequiredProperty(property, "minBounds").vector2Value = settings.MinBounds;
        DataImportAssetUtility.FindRequiredProperty(property, "maxBounds").vector2Value = settings.MaxBounds;
        WriteObstacleLayerNames(DataImportAssetUtility.FindRequiredProperty(property, "obstacleLayerNames"), settings.ObstacleLayerNames);
    }

    private static void WriteObstacleLayerNames(SerializedProperty property, IReadOnlyList<string> layerNames)
    {
        property.arraySize = layerNames.Count;
        for (int i = 0; i < layerNames.Count; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = layerNames[i];
        }
    }

    private static float ResolveMinDistance(WaveJsonSpawnLocationStrategy data)
    {
        return data.minDistance > 0f ? data.minDistance : 6f;
    }

    private static float ResolveMaxDistance(WaveJsonSpawnLocationStrategy data)
    {
        return data.maxDistance > 0f ? data.maxDistance : 10f;
    }

    private static float ResolveBoundsPadding(WaveJsonSpawnLocationResolverSettings data)
    {
        return data.boundsPadding >= 0f ? data.boundsPadding : 1f;
    }

    private static int ResolveAttempts(WaveJsonSpawnLocationResolverSettings data)
    {
        return data.resolveAttempts > 0 ? data.resolveAttempts : 16;
    }

    private static float ResolveSpawnClearance(WaveJsonSpawnLocationResolverSettings data)
    {
        return data.spawnClearance >= 0f ? data.spawnClearance : 0.1f;
    }

    private static Vector2 ToVector2(WaveJsonVector2 value, Vector2 fallback)
    {
        return value != null ? new Vector2(value.x, value.y) : fallback;
    }

    private static string[] ToLayerNames(WaveJsonSpawnLocationResolverSettings data)
    {
        if (data.obstacleLayerNames == null || data.obstacleLayerNames.Count == 0)
        {
            return new[] { "Wall" };
        }

        List<string> names = new(data.obstacleLayerNames.Count);
        for (int i = 0; i < data.obstacleLayerNames.Count; i++)
        {
            string layerName = data.obstacleLayerNames[i];
            if (!string.IsNullOrWhiteSpace(layerName))
            {
                names.Add(layerName.Trim());
            }
        }

        return names.Count > 0 ? names.ToArray() : new[] { "Wall" };
    }

    private static TEnum ParseEnum<TEnum>(string value, string waveId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{waveId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, WaveDefinitionSO> LoadWavesById()
    {
        Dictionary<string, WaveDefinitionSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<WaveDefinitionSO> assets = DataImportAssetUtility.LoadAssets<WaveDefinitionSO>(WaveFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            WaveDefinitionSO wave = assets[i];
            if (wave == null || string.IsNullOrWhiteSpace(wave.WaveId))
            {
                continue;
            }

            result[wave.WaveId] = wave;
        }

        return result;
    }

    private static string BuildWavePath(string waveId)
    {
        return $"{WaveFolder}/{DataImportAssetUtility.ToSafeAssetFileName(waveId)}.asset";
    }
}
#endif
