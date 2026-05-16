using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class UpgradeCardAssetTests
{
    private const string CardFolder = GameContentAssetPaths.UpgradeCards;
    private const string PoolPath = GameContentAssetPaths.UpgradeCardPool;

    [Test]
    public void UpgradeCardJsonRowsAreReadableAndUnique()
    {
        IReadOnlyList<UpgradeCardJsonCard> rows = UpgradeCardJsonReader.ReadDefault();
        Assert.AreEqual(79, rows.Count);

        HashSet<string> cardIds = new(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; i++)
        {
            UpgradeCardJsonCard row = rows[i];
            Assert.IsTrue(cardIds.Add(row.cardId), $"Duplicated cardId: {row.cardId}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.title), row.cardId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.rarity), row.cardId);
            Assert.NotNull(row.tags, row.cardId);
            Assert.NotNull(row.specialFeatures, row.cardId);
        }
    }

    [Test]
    public void UpgradeCardsMatchJsonRowsAndUsePropertyModifierFeatures()
    {
        IReadOnlyList<UpgradeCardJsonCard> rows = UpgradeCardJsonReader.ReadDefault();
        Dictionary<string, UpgradeCardJsonCard> rowsById = ToRowsById(rows);
        UpgradeCardSO[] cards = LoadUpgradeCards();
        Assert.AreEqual(rows.Count, cards.Length);

        HashSet<string> cardIds = new();
        for (int i = 0; i < cards.Length; i++)
        {
            UpgradeCardSO card = cards[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.CardId), card.name);
            Assert.IsTrue(cardIds.Add(card.CardId), $"Duplicated cardId: {card.CardId}");
            Assert.IsTrue(rowsById.TryGetValue(card.CardId, out UpgradeCardJsonCard row), card.CardId);
            Assert.AreEqual(row.title, card.Title, card.CardId);
            Assert.AreEqual(ParseEnum<UpgradeCardRarity>(row.rarity), card.Rarity, card.CardId);
            Assert.AreEqual(ResolveTags(row), card.Tags, card.CardId);
            Assert.IsNull(card.Icon, card.CardId);

            UpgradeCardOptionViewData viewData = card.CreateOptionViewData(0, 0);
            Assert.IsNull(viewData.Icon, card.CardId);

            Assert.AreEqual(row.specialFeatures.Count, card.SpecialFeatures.Count, card.CardId);
            for (int featureIndex = 0; featureIndex < row.specialFeatures.Count; featureIndex++)
            {
                AssertJsonFeatureMatchesRuntimeFeature(row.cardId, row.specialFeatures[featureIndex], card.SpecialFeatures[featureIndex]);
            }
        }
    }

    [Test]
    public void UpgradeCardPoolUsesJsonCardsWithRarityMetadataAndUnlimitedPicks()
    {
        IReadOnlyList<UpgradeCardJsonCard> rows = UpgradeCardJsonReader.ReadDefault();
        UpgradeCardSO[] cards = LoadUpgradeCards();
        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(PoolPath);
        Assert.NotNull(pool);
        Assert.AreEqual(4, pool.DefaultRollCount);
        Assert.IsFalse(pool.AllowDuplicateResults);
        Assert.AreEqual(rows.Count, cards.Length);

        Dictionary<string, UpgradeCardSO> cardsById = ToCardsById(cards);
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            ContentPoolEntry entry = pool.Entries[i];
            UpgradeCardSO card = entry.Content as UpgradeCardSO;
            Assert.NotNull(card, entry.EntryId);
            Assert.IsTrue(cardsById.ContainsKey(card.CardId), entry.EntryId);
            Assert.AreEqual(card.CardId, entry.EntryId);
            Assert.AreEqual(UpgradeCardSO.UNLIMITED_PICK_COUNT, entry.MaxPickCount);
            Assert.IsTrue(entry.TryGetMetadata(out QualityMetadata qualityMetadata), entry.EntryId);
            Assert.AreEqual((int)card.Rarity, qualityMetadata.QualityValue, entry.EntryId);
        }
    }

    [Test]
    public void UpgradeCardPoolBuilderDoesNotFilterByFeatureType()
    {
        UpgradeCardSO mechanicCard = ScriptableObject.CreateInstance<UpgradeCardSO>();
        mechanicCard.name = "Mechanic Card Test";
        mechanicCard.InitializeRuntime(
            "mechanic_card",
            "Mechanic Card",
            UpgradeCardRarity.Common,
            Array.Empty<UpgradeCardTag>(),
            string.Empty,
            new FeatureBase[]
            {
                new TestFeature()
            });

        try
        {
            ContentPoolEntry entry = UpgradeCardContentPoolTuningUtility.CreateEntry(mechanicCard);

            Assert.NotNull(entry);
            Assert.AreSame(mechanicCard, entry.Content);
            Assert.AreEqual(mechanicCard.CardId, entry.EntryId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mechanicCard);
        }
    }

    private static UpgradeCardSO[] LoadUpgradeCards()
    {
        string[] guids = AssetDatabase.FindAssets("t:UpgradeCardSO", new[] { CardFolder });
        List<UpgradeCardSO> cards = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            UpgradeCardSO card = AssetDatabase.LoadAssetAtPath<UpgradeCardSO>(path);
            if (card != null)
            {
                cards.Add(card);
            }
        }

        return cards.ToArray();
    }

    private static Dictionary<string, UpgradeCardJsonCard> ToRowsById(IReadOnlyList<UpgradeCardJsonCard> rows)
    {
        Dictionary<string, UpgradeCardJsonCard> result = new(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.IsTrue(result.TryAdd(rows[i].cardId, rows[i]), $"Duplicated upgrade card JSON id: {rows[i].cardId}");
        }

        return result;
    }

    private static Dictionary<string, UpgradeCardSO> ToCardsById(IReadOnlyList<UpgradeCardSO> cards)
    {
        Dictionary<string, UpgradeCardSO> result = new(StringComparer.Ordinal);
        for (int i = 0; i < cards.Count; i++)
        {
            Assert.IsTrue(result.TryAdd(cards[i].CardId, cards[i]), $"Duplicated upgrade card asset id: {cards[i].CardId}");
        }

        return result;
    }

    private static void AssertJsonFeatureMatchesRuntimeFeature(
        string cardId,
        UpgradeCardJsonFeature jsonFeature,
        FeatureBase runtimeFeature)
    {
        Assert.AreEqual(nameof(PropertyModifierFeature), jsonFeature.type, cardId);
        PropertyModifierFeature propertyFeature = runtimeFeature as PropertyModifierFeature;
        Assert.NotNull(propertyFeature, cardId);
        Assert.NotNull(jsonFeature.modifier, cardId);

        PropModifierData modifier = propertyFeature.Modifier;
        Assert.AreEqual(ParseEnum<PropType>(jsonFeature.modifier.propType), modifier.propType, cardId);
        Assert.AreEqual(ParseEnum<PropModifierType>(jsonFeature.modifier.modifierType), modifier.modifierType, cardId);
        Assert.That(modifier.value, Is.EqualTo(jsonFeature.modifier.value).Within(0.0001f), cardId);
    }

    private static UpgradeCardTag ResolveTags(UpgradeCardJsonCard row)
    {
        UpgradeCardTag tags = UpgradeCardTag.None;
        for (int i = 0; i < row.tags.Count; i++)
        {
            tags |= ParseEnum<UpgradeCardTag>(row.tags[i]);
        }

        return tags;
    }

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct
    {
        Assert.IsTrue(Enum.TryParse(value, true, out TEnum result), $"Cannot parse '{value}' as {typeof(TEnum).Name}.");
        return result;
    }

    private sealed class TestFeature : FeatureBase
    {
    }
}
