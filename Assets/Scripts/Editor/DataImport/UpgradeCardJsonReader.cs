#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class UpgradeCardJsonReader
{
    public const string DefaultJsonPath = "Assets/GameContent/Upgrades/Data/upgrade_cards.json";

    public static IReadOnlyList<UpgradeCardJsonCard> ReadDefault()
    {
        return Read(DefaultJsonPath);
    }

    public static IReadOnlyList<UpgradeCardJsonCard> Read(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new ArgumentException("Upgrade card JSON path must not be empty.", nameof(jsonPath));
        }

        string absolutePath = Path.GetFullPath(jsonPath);
        if (!File.Exists(absolutePath))
        {
            throw new DataImportException($"Upgrade card JSON does not exist: {absolutePath}");
        }

        try
        {
            string json = File.ReadAllText(absolutePath, Encoding.UTF8);
            UpgradeCardJsonFile file = JsonUtility.FromJson<UpgradeCardJsonFile>(WrapRootArray(json));
            if (file?.cards == null)
            {
                throw new DataImportException($"Upgrade card JSON has no cards array: {absolutePath}");
            }

            Validate(file.cards, absolutePath);
            return file.cards;
        }
        catch (DataImportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DataImportException($"Failed to read upgrade card JSON: {absolutePath}", exception);
        }
    }

    private static string WrapRootArray(string json)
    {
        string normalized = (json ?? string.Empty).Trim();
        if (normalized.StartsWith("[", StringComparison.Ordinal))
        {
            return "{\"cards\":" + normalized + "}";
        }

        return normalized;
    }

    private static void Validate(IReadOnlyList<UpgradeCardJsonCard> cards, string context)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardJsonCard card = cards[i];
            string cardContext = $"{context} card[{i}]";
            if (card == null)
            {
                throw new DataImportException($"{cardContext} is null.");
            }

            Require(card.cardId, cardContext, nameof(card.cardId));
            Require(card.title, cardContext, nameof(card.title));
            Require(card.rarity, cardContext, nameof(card.rarity));
            if (!ids.Add(card.cardId))
            {
                throw new DataImportException($"{cardContext} has duplicated cardId '{card.cardId}'.");
            }

            if (card.tags == null)
            {
                throw new DataImportException($"{card.cardId} has null tags.");
            }

            if (card.specialFeatures == null)
            {
                throw new DataImportException($"{card.cardId} has null specialFeatures.");
            }

            for (int featureIndex = 0; featureIndex < card.specialFeatures.Count; featureIndex++)
            {
                UpgradeCardJsonFeature feature = card.specialFeatures[featureIndex];
                if (feature == null)
                {
                    throw new DataImportException($"{card.cardId} specialFeatures[{featureIndex}] is null.");
                }

                Require(feature.type, $"{card.cardId} specialFeatures[{featureIndex}]", nameof(feature.type));
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
