#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class BuffJsonReader
{
    public const string DefaultJsonPath = GameContentAssetPaths.CombatBuffs + "/buffs.json";

    public static IReadOnlyList<BuffJsonBuff> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<BuffJsonBuff> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Buff JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Buff JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            BuffJsonFile file = JsonUtility.FromJson<BuffJsonFile>(WrapRootArray(json));
            if (file?.buffs == null)
            {
                throw new DataImportException($"Buff JSON has no buffs array: {absolutePath}");
            }

            Validate(file.buffs, absolutePath);
            return file.buffs;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read buff JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"buffs\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<BuffJsonBuff> buffs, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffJsonBuff buff = buffs[i];
            string buffContext = $"{context} buff[{i}]";
            if (buff == null)
            {
                throw new DataImportException($"{buffContext} is null.");
            }

            Require(buff.buffId, buffContext, nameof(buff.buffId));
            Require(buff.displayName, buffContext, nameof(buff.displayName));
            Require(buff.polarity, buffContext, nameof(buff.polarity));
            Require(buff.durationPolicy, buffContext, nameof(buff.durationPolicy));
            Require(buff.refreshMode, buffContext, nameof(buff.refreshMode));
            Require(buff.overflowMode, buffContext, nameof(buff.overflowMode));
            if (!ids.Add(buff.buffId))
            {
                throw new DataImportException($"{buffContext} has duplicated buffId '{buff.buffId}'.");
            }

            if (buff.specialFeatures == null)
            {
                throw new DataImportException($"{buff.buffId} has null specialFeatures.");
            }

            for (int featureIndex = 0; featureIndex < buff.specialFeatures.Count; featureIndex++)
            {
                BuffJsonFeature feature = buff.specialFeatures[featureIndex];
                if (feature == null)
                {
                    throw new DataImportException($"{buff.buffId} specialFeatures[{featureIndex}] is null.");
                }

                Require(feature.type, $"{buff.buffId} specialFeatures[{featureIndex}]", nameof(feature.type));
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
