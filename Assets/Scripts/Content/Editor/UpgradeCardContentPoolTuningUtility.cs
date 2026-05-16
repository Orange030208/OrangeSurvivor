#if UNITY_EDITOR
using System.Collections.Generic;

public static class UpgradeCardContentPoolTuningUtility
{
    public static ContentPoolEntry CreateEntry(UpgradeCardSO card, float baseWeight = 1f)
    {
        if (card == null)
        {
            return null;
        }

        ContentPoolEntry entry = new(card, baseWeight, card.CardId);
        entry.ConfigureRuntimeLimits(0, UpgradeCardSO.UNLIMITED_PICK_COUNT);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new QualityMetadata((int)card.Rarity)
        });
        entry.ConfigureRuntimeRules(BuildUpgradeCardConditions(), BuildUpgradeCardWeightRules());
        return entry;
    }

    private static List<ContentCondition> BuildUpgradeCardConditions()
    {
        return new List<ContentCondition>
        {
            new CurrentWaveCondition(ContentComparisonOperator.GreaterOrEqual, 1)
        };
    }

    private static List<ContentWeightRule> BuildUpgradeCardWeightRules()
    {
        return new List<ContentWeightRule>
        {
            new PreviousRollWeightContentRule(1f)
        };
    }
}
#endif
