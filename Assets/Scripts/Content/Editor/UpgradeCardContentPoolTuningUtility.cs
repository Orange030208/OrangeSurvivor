#if UNITY_EDITOR
using System.Collections.Generic;

public static class UpgradeCardContentPoolTuningUtility
{
    private const float DefaultPreviousOfferMultiplier = 0.5f;

    public static ContentPoolEntry CreateEntry(UpgradeCardSO card)
    {
        if (card == null)
        {
            return null;
        }

        ContentPoolEntry entry = new(card, GetBaseWeight(card.Rarity), card.CardId);
        entry.ConfigureRuntimeLimits(0, UpgradeCardSO.UNLIMITED_PICK_COUNT);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new QualityMetadata((int)card.Rarity)
        });
        entry.ConfigureRuntimeRules(null, BuildUpgradeCardWeightRules());
        return entry;
    }

    private static float GetBaseWeight(UpgradeCardRarity rarity)
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

    private static List<ContentWeightRule> BuildUpgradeCardWeightRules()
    {
        return new List<ContentWeightRule>
        {
            new PreviousRollWeightContentRule(DefaultPreviousOfferMultiplier)
        };
    }
}
#endif
