#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UpgradeCardJsonAssetSync
{
    private const string CardFolder = GameContentAssetPaths.UpgradeCards;
    private const string UpgradeCardPoolPath = GameContentAssetPaths.UpgradeCardPool;

    public static DataImportReport Preview(IReadOnlyList<UpgradeCardJsonCard> cards)
    {
        DataImportReport report = new();
        Dictionary<string, UpgradeCardSO> assetsById = LoadCardsById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);

        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardJsonCard card = cards[i];
            ValidateCard(card);
            if (!jsonIds.Add(card.cardId))
            {
                report.AddBlocker($"Duplicated cardId in JSON: {card.cardId}");
                continue;
            }

            if (assetsById.TryGetValue(card.cardId, out UpgradeCardSO asset))
            {
                report.AddUpdated($"{card.cardId} -> {AssetDatabase.GetAssetPath(asset)}");
            }
            else
            {
                report.AddCreated($"{card.cardId} -> {BuildCardPath(card.cardId)}");
            }
        }

        foreach (KeyValuePair<string, UpgradeCardSO> pair in assetsById)
        {
            if (!jsonIds.Contains(pair.Key))
            {
                report.AddDeleteCandidate($"{pair.Key} -> {AssetDatabase.GetAssetPath(pair.Value)}");
            }
        }

        return report;
    }

    public static DataImportReport Apply(IReadOnlyList<UpgradeCardJsonCard> cards)
    {
        DataImportReport report = Preview(cards);
        if (report.HasBlockers)
        {
            return report;
        }

        DataImportAssetUtility.EnsureFolder(CardFolder);
        Dictionary<string, UpgradeCardSO> assetsById = LoadCardsById();
        HashSet<string> jsonIds = new(StringComparer.Ordinal);
        for (int i = 0; i < cards.Count; i++)
        {
            jsonIds.Add(cards[i].cardId);
            UpgradeCardJsonCard cardData = cards[i];
            if (!assetsById.TryGetValue(cardData.cardId, out UpgradeCardSO card))
            {
                card = ScriptableObject.CreateInstance<UpgradeCardSO>();
                card.name = cardData.cardId;
                AssetDatabase.CreateAsset(card, BuildCardPath(cardData.cardId));
                assetsById[cardData.cardId] = card;
            }

            ApplyCard(card, cardData);
        }

        foreach (KeyValuePair<string, UpgradeCardSO> pair in assetsById)
        {
            if (jsonIds.Contains(pair.Key))
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(pair.Value);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                report.AddBlocker($"Cannot delete stale upgrade card '{pair.Key}' because its asset path is missing.");
                continue;
            }

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                report.AddBlocker($"Failed to delete stale upgrade card asset: {assetPath}");
            }
        }

        RebuildUpgradeCardPool();
        AssetDatabase.SaveAssets();
        return report;
    }

    private static void ValidateCard(UpgradeCardJsonCard card)
    {
        ParseEnum<UpgradeCardRarity>(card.rarity, card.cardId, nameof(card.rarity));
        ResolveTags(card);
        for (int i = 0; i < card.specialFeatures.Count; i++)
        {
            CreateFeature(card.cardId, i, card.specialFeatures[i]);
        }
    }

    private static void ApplyCard(UpgradeCardSO card, UpgradeCardJsonCard data)
    {
        SerializedObject serializedObject = new(card);
        DataImportAssetUtility.SetString(serializedObject, "cardId", data.cardId);
        DataImportAssetUtility.SetString(serializedObject, "title", data.title);
        DataImportAssetUtility.SetEnum(serializedObject, "rarity", ParseEnum<UpgradeCardRarity>(data.rarity, data.cardId, nameof(data.rarity)));
        DataImportAssetUtility.SetEnum(serializedObject, "tags", ResolveTags(data));
        DataImportAssetUtility.SetString(serializedObject, "description", data.description);
        WriteSpecialFeatures(serializedObject, data);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(card);
    }

    private static void WriteSpecialFeatures(SerializedObject serializedObject, UpgradeCardJsonCard card)
    {
        SerializedProperty property = DataImportAssetUtility.FindRequiredProperty(serializedObject, "specialFeatures");
        property.arraySize = card.specialFeatures.Count;
        for (int i = 0; i < card.specialFeatures.Count; i++)
        {
            property.GetArrayElementAtIndex(i).managedReferenceValue = CreateFeature(card.cardId, i, card.specialFeatures[i]);
        }
    }

    private static FeatureBase CreateFeature(string cardId, int index, UpgradeCardJsonFeature data)
    {
        return data.type switch
        {
            nameof(PropertyModifierFeature) => CreatePropertyModifierFeature(cardId, index, data),
            _ => throw new DataImportException($"{cardId} specialFeatures[{index}] has unsupported feature type '{data.type}'.")
        };
    }

    private static PropertyModifierFeature CreatePropertyModifierFeature(string cardId, int index, UpgradeCardJsonFeature data)
    {
        if (data.modifier == null)
        {
            throw new DataImportException($"{cardId} specialFeatures[{index}] is missing modifier.");
        }

        PropType propType = ParseEnum<PropType>(data.modifier.propType, cardId, "modifier.propType");
        PropModifierType modifierType = ParseEnum<PropModifierType>(data.modifier.modifierType, cardId, "modifier.modifierType");
        return new PropertyModifierFeature(new PropModifierData(propType, modifierType, data.modifier.value));
    }

    private static UpgradeCardTag ResolveTags(UpgradeCardJsonCard card)
    {
        UpgradeCardTag result = UpgradeCardTag.None;
        for (int i = 0; i < card.tags.Count; i++)
        {
            string value = card.tags[i];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result |= ParseEnum<UpgradeCardTag>(value, card.cardId, $"tags[{i}]");
        }

        return result;
    }

    private static TEnum ParseEnum<TEnum>(string value, string cardId, string fieldName)
        where TEnum : struct
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _)
            && Enum.TryParse(value, true, out TEnum result)
            && Enum.IsDefined(typeof(TEnum), result))
        {
            return result;
        }

        throw new DataImportException($"{cardId} cannot parse '{value}' as {typeof(TEnum).Name} for field '{fieldName}'.");
    }

    private static Dictionary<string, UpgradeCardSO> LoadCardsById()
    {
        Dictionary<string, UpgradeCardSO> result = new(StringComparer.Ordinal);
        IReadOnlyList<UpgradeCardSO> assets = DataImportAssetUtility.LoadAssets<UpgradeCardSO>(CardFolder);
        for (int i = 0; i < assets.Count; i++)
        {
            UpgradeCardSO card = assets[i];
            if (card == null || string.IsNullOrWhiteSpace(card.CardId))
            {
                continue;
            }

            result[card.CardId] = card;
        }

        return result;
    }

    private static void RebuildUpgradeCardPool()
    {
        DataImportAssetUtility.EnsureFolder(GameContentAssetPaths.UpgradePools);
        List<ContentPoolEntry> entries = new();
        IReadOnlyList<UpgradeCardSO> cards = DataImportAssetUtility.LoadAssets<UpgradeCardSO>(CardFolder);
        for (int i = 0; i < cards.Count; i++)
        {
            ContentPoolEntry entry = UpgradeCardContentPoolTuningUtility.CreateEntry(cards[i]);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(UpgradeCardPoolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<ContentPoolSO>();
            AssetDatabase.CreateAsset(pool, UpgradeCardPoolPath);
        }

        pool.Initialize(entries, 4, false);
        EditorUtility.SetDirty(pool);
    }

    private static string BuildCardPath(string cardId)
    {
        return $"{CardFolder}/{DataImportAssetUtility.ToSafeAssetFileName(cardId)}.asset";
    }
}
#endif
