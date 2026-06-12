using System;
using Orange.Extraction;

/// <summary>
/// Applies the configured luck-based weight delta using the per-luck-point values stored in a content weight profile.
/// </summary>
public sealed class ContentTierLuckWeightModifier<TItem, TContext> : IExtractionWeightModifier<TItem, TContext>
{
    private readonly ContentTierWeightProfileSO weightProfile;
    private readonly Func<TItem, ContentTier> tierSelector;
    private readonly Func<TContext, float> luckSelector;

    public ContentTierLuckWeightModifier(
        ContentTierWeightProfileSO weightProfile,
        Func<TItem, ContentTier> tierSelector,
        Func<TContext, float> luckSelector)
    {
        this.weightProfile = weightProfile ?? throw new ArgumentNullException(nameof(weightProfile));
        this.tierSelector = tierSelector ?? throw new ArgumentNullException(nameof(tierSelector));
        this.luckSelector = luckSelector;
    }

    public float ModifyWeight(WeightedExtractionEntry<TItem, TContext> entry, TContext context)
    {
        if (entry == null)
        {
            return 0f;
        }

        float luck = luckSelector != null ? luckSelector(context) : 0f;
        ContentTier tier = tierSelector(entry.Item);
        float weightPerLuckPoint = weightProfile.GetWeightPerLuckPoint(tier);
        return entry.CurrentWeight + (luck * weightPerLuckPoint);
    }
}
