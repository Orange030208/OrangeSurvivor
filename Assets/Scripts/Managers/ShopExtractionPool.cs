using System.Collections.Generic;
using Orange.Extraction;
using System;

public sealed class ShopExtractionPool : WeightedExtractionPool<ShopExtractionCandidate, ShopExtractionContext>
{
    public ShopExtractionPool(
        IEnumerable<ShopExtractionCandidate> candidates,
        ContentTierWeightProfileSO tierWeightProfile,
        IExtractionRandom random = null)
        : base(random)
    {
        ContentTierWeightProfileSO resolvedTierWeightProfile =
            tierWeightProfile ?? throw new ArgumentNullException(nameof(tierWeightProfile));
        AddWeightModifier(
            new ContentTierLuckWeightModifier<ShopExtractionCandidate, ShopExtractionContext>(
                resolvedTierWeightProfile,
                candidate => candidate.Tier,
                context => context.Luck));
        AddCandidates(candidates, resolvedTierWeightProfile);
    }

    private void AddCandidates(
        IEnumerable<ShopExtractionCandidate> candidates,
        ContentTierWeightProfileSO tierWeightProfile)
    {
        if (candidates == null)
        {
            return;
        }

        foreach (ShopExtractionCandidate candidate in candidates)
        {
            if (candidate == null || candidate.ItemData == null)
            {
                continue;
            }

            AddEntry(
                candidate.EntryId,
                candidate,
                tierWeightProfile.GetWeight(candidate.Tier),
                IsEligible);
        }
    }

    private static bool IsEligible(
        WeightedExtractionEntry<ShopExtractionCandidate, ShopExtractionContext> entry,
        ShopExtractionContext context)
    {
        if (entry?.Item?.ItemData is not AccessoryDataSO accessory)
        {
            return true;
        }

        return context.AccessoryManager == null || context.AccessoryManager.CanEquipAccessory(accessory);
    }
}
