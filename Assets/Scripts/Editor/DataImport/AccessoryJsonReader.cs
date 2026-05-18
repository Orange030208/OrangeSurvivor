#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class AccessoryJsonReader
{
    public const string DefaultJsonPath = "Assets/GameContent/Accessories/Data/accessories.json";

    public static IReadOnlyList<AccessoryJsonAccessory> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<AccessoryJsonAccessory> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Accessory JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Accessory JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            AccessoryJsonFile file = JsonUtility.FromJson<AccessoryJsonFile>(WrapRootArray(json));
            if (file?.accessories == null)
            {
                throw new DataImportException($"Accessory JSON has no accessories array: {absolutePath}");
            }

            Validate(file.accessories, absolutePath);
            return file.accessories;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read accessory JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"accessories\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<AccessoryJsonAccessory> accessories, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < accessories.Count; i++)
        {
            AccessoryJsonAccessory accessory = accessories[i];
            string accessoryContext = $"{context} accessory[{i}]";
            if (accessory == null)
            {
                throw new DataImportException($"{accessoryContext} is null.");
            }

            Require(accessory.accessoryId, accessoryContext, nameof(accessory.accessoryId));
            Require(accessory.itemName, accessoryContext, nameof(accessory.itemName));
            Require(accessory.rarity, accessoryContext, nameof(accessory.rarity));
            if (!ids.Add(accessory.accessoryId))
            {
                throw new DataImportException($"{accessoryContext} has duplicated accessoryId '{accessory.accessoryId}'.");
            }

            if (accessory.propertyModifiers == null)
            {
                throw new DataImportException($"{accessory.accessoryId} has null propertyModifiers.");
            }

            if (accessory.specialFeatures == null)
            {
                throw new DataImportException($"{accessory.accessoryId} has null specialFeatures.");
            }

            for (int featureIndex = 0; featureIndex < accessory.specialFeatures.Count; featureIndex++)
            {
                AccessoryJsonFeature feature = accessory.specialFeatures[featureIndex];
                if (feature == null)
                {
                    throw new DataImportException($"{accessory.accessoryId} specialFeatures[{featureIndex}] is null.");
                }

                Require(feature.type, $"{accessory.accessoryId} specialFeatures[{featureIndex}]", nameof(feature.type));
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
