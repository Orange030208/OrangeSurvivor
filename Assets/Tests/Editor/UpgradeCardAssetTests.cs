using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

public class UpgradeCardAssetTests
{
    private const string CardFolder = GameContentAssetPaths.UpgradeCards;
    private const string PoolPath = GameContentAssetPaths.UpgradeCardPool;
    private const float BudgetTolerance = 0.1f;

    [Test]
    public void UpgradeCardsUseChineseIdsAndPropertyModifierFeatures()
    {
        UpgradeCardSO[] cards = LoadUpgradeCards();
        Assert.AreEqual(43, cards.Length);

        HashSet<string> cardIds = new();
        for (int i = 0; i < cards.Length; i++)
        {
            UpgradeCardSO card = cards[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.CardId), card.name);
            Assert.IsTrue(ContainsChinese(card.CardId), card.CardId);
            Assert.IsTrue(cardIds.Add(card.CardId), $"Duplicated cardId: {card.CardId}");
            IReadOnlyList<PropertyModifierFeature> propertyFeatures = GetPropertyModifierFeatures(card);
            Assert.Greater(propertyFeatures.Count, 0, card.CardId);

            if (card.Rarity != UpgradeCardRarity.Legendary)
            {
                Assert.AreEqual(1, propertyFeatures.Count, card.CardId);
            }
        }
    }

    [Test]
    public void UpgradeCardsStayWithinBudgetTolerance()
    {
        UpgradeCardSO[] cards = LoadUpgradeCards();
        for (int i = 0; i < cards.Length; i++)
        {
            UpgradeCardSO card = cards[i];
            float budget = CalculateBudget(card);
            float target = GetRarityBudget(card.Rarity);
            float min = target * (1f - BudgetTolerance);
            float max = target * (1f + BudgetTolerance);

            Assert.IsTrue(
                budget >= min && budget <= max,
                $"{card.CardId} {card.Rarity} budget {budget} is outside {min}-{max}.");
        }
    }

    [Test]
    public void UpgradeCardPoolUsesNewCardsWithRarityWeightsAndUnlimitedPicks()
    {
        UpgradeCardSO[] cards = LoadUpgradeCards();
        ContentPoolSO pool = AssetDatabase.LoadAssetAtPath<ContentPoolSO>(PoolPath);
        Assert.NotNull(pool);
        Assert.AreEqual(4, pool.DefaultRollCount);
        Assert.IsFalse(pool.AllowDuplicateResults);
        Assert.AreEqual(cards.Length, pool.Entries.Count);

        HashSet<UpgradeCardSO> cardSet = new(cards);
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            ContentPoolEntry entry = pool.Entries[i];
            UpgradeCardSO card = entry.Content as UpgradeCardSO;
            Assert.NotNull(card, entry.EntryId);
            Assert.IsTrue(cardSet.Contains(card), entry.EntryId);
            Assert.AreEqual(card.CardId, entry.EntryId);
            Assert.AreEqual(GetRarityWeight(card.Rarity), entry.BaseWeight);
            Assert.AreEqual(UpgradeCardSO.UNLIMITED_PICK_COUNT, entry.MaxPickCount);
            Assert.IsTrue(entry.TryGetMetadata(out QualityMetadata qualityMetadata), entry.EntryId);
            Assert.AreEqual((int)card.Rarity, qualityMetadata.QualityValue, entry.EntryId);
            Assert.AreEqual(0, entry.Conditions.Count, card.CardId);
            Assert.AreEqual(1, entry.WeightRules.Count, card.CardId);
            Assert.IsInstanceOf<PreviousRollWeightContentRule>(entry.WeightRules[0], card.CardId);
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

    private static bool ContainsChinese(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c >= '\u4e00' && c <= '\u9fff')
            {
                return true;
            }
        }

        return false;
    }

    private static float CalculateBudget(UpgradeCardSO card)
    {
        float budget = 0f;
        IReadOnlyList<PropertyModifierFeature> propertyFeatures = GetPropertyModifierFeatures(card);
        for (int i = 0; i < propertyFeatures.Count; i++)
        {
            PropModifierData modifier = propertyFeatures[i].Modifier;
            Assert.AreEqual(PropModifierType.Add, modifier.modifierType, card.CardId);
            budget += modifier.value * GetPointValue(modifier.propType);
        }

        return budget;
    }

    private static IReadOnlyList<PropertyModifierFeature> GetPropertyModifierFeatures(UpgradeCardSO card)
    {
        List<PropertyModifierFeature> propertyFeatures = new();
        IReadOnlyList<FeatureEffectBase> specialFeatures = card.SpecialFeatures;
        for (int i = 0; i < specialFeatures.Count; i++)
        {
            if (specialFeatures[i] is PropertyModifierFeature propertyFeature)
            {
                propertyFeatures.Add(propertyFeature);
                continue;
            }

            Assert.Fail($"{card.CardId} has non-property upgrade feature {specialFeatures[i]?.GetType().Name}.");
        }

        return propertyFeatures;
    }

    private static float GetPointValue(PropType propType)
    {
        return propType switch
        {
            PropType.Damage => 20f,
            PropType.MeleeAttack => 10f,
            PropType.RangedAttack => 10f,
            PropType.MagicAttack => 10f,
            PropType.SummonAttack => 10f,
            PropType.AttackSpeed => 20f,
            PropType.CriticalChance => 50f,
            PropType.CriticalPercent => 12f,
            PropType.MoveSpeed => 10f,
            PropType.MaxHealth => 8f,
            PropType.HealthRecoverySpeed => 5f,
            PropType.Armor => 80f,
            PropType.Luck => 20f,
            PropType.Dodge => 33f,
            PropType.PickupRadius => 8f,
            PropType.AttackRange => 10f,
            PropType.DamageReduction => 35f,
            _ => throw new AssertionException($"Missing point value for {propType}")
        };
    }

    private static float GetRarityBudget(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => 100f,
            UpgradeCardRarity.Rare => 150f,
            UpgradeCardRarity.Epic => 225f,
            UpgradeCardRarity.Legendary => 350f,
            _ => 0f
        };
    }

    private static float GetRarityWeight(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => 100f,
            UpgradeCardRarity.Rare => 45f,
            UpgradeCardRarity.Epic => 12f,
            UpgradeCardRarity.Legendary => 3f,
            _ => 0f
        };
    }
}
