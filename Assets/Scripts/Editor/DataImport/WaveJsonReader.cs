#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class WaveJsonReader
{
    public const string DefaultJsonPath = GameContentAssetPaths.WavesData + "/waves.json";

    public static IReadOnlyList<WaveJsonWave> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<WaveJsonWave> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Wave JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Wave JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            WaveJsonFile file = JsonUtility.FromJson<WaveJsonFile>(WrapRootArray(json));
            if (file?.waves == null)
            {
                throw new DataImportException($"Wave JSON has no waves array: {absolutePath}");
            }

            Validate(file.waves, absolutePath);
            return file.waves;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read wave JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"waves\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<WaveJsonWave> waves, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < waves.Count; i++)
        {
            WaveJsonWave wave = waves[i];
            string waveContext = $"{context} waves[{i}]";
            if (wave == null)
            {
                throw new DataImportException($"{waveContext} is null.");
            }

            Require(wave.waveId, waveContext, nameof(wave.waveId));
            Require(wave.displayName, waveContext, nameof(wave.displayName));
            Require(wave.completionMode, waveContext, nameof(wave.completionMode));
            if (wave.spawnLocation == null)
            {
                throw new DataImportException($"{waveContext} is missing required field '{nameof(wave.spawnLocation)}'.");
            }

            if (wave.spawnLocation.resolverSettings == null)
            {
                throw new DataImportException($"{waveContext} is missing required field '{nameof(wave.spawnLocation)}.{nameof(wave.spawnLocation.resolverSettings)}'.");
            }

            if (wave.spawnLocation.strategy == null)
            {
                throw new DataImportException($"{waveContext} is missing required field '{nameof(wave.spawnLocation)}.{nameof(wave.spawnLocation.strategy)}'.");
            }

            Require(wave.spawnLocation.strategy.type, $"{waveContext}.{nameof(wave.spawnLocation)}.{nameof(wave.spawnLocation.strategy)}", nameof(wave.spawnLocation.strategy.type));
            if (!ids.Add(wave.waveId))
            {
                throw new DataImportException($"{waveContext} has duplicated waveId '{wave.waveId}'.");
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
