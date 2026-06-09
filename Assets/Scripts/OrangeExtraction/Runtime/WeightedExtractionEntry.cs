using System;

namespace Orange.Extraction
{
    public class WeightedExtractionEntry<TItem, TContext>
    {
        public WeightedExtractionEntry(
            string entryId,
            TItem item,
            float baseWeight,
            ExtractionEligibility<TItem, TContext> eligibility = null,
            IExtractionWeightModifier<TItem, TContext> weightModifier = null)
        {
            if (string.IsNullOrWhiteSpace(entryId))
            {
                throw new ArgumentException("Extraction entry id cannot be null, empty, or whitespace.", nameof(entryId));
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item), $"Extraction entry '{entryId}' cannot hold a null item.");
            }

            ExtractionValidation.ThrowIfInvalidBaseWeight(baseWeight, entryId);

            EntryId = entryId;
            Item = item;
            BaseWeight = baseWeight;
            Eligibility = eligibility;
            WeightModifier = weightModifier;
        }

        public string EntryId { get; }
        public TItem Item { get; }
        public float BaseWeight { get; }
        public ExtractionEligibility<TItem, TContext> Eligibility { get; }
        public IExtractionWeightModifier<TItem, TContext> WeightModifier { get; }

        public bool IsEligible(TContext context)
        {
            return Eligibility == null || Eligibility(this, context);
        }
    }

    public sealed class WeightedExtractionEntry<TItem> : WeightedExtractionEntry<TItem, EmptyExtractionContext>
    {
        public WeightedExtractionEntry(
            string entryId,
            TItem item,
            float baseWeight,
            ExtractionEligibility<TItem, EmptyExtractionContext> eligibility = null,
            IExtractionWeightModifier<TItem, EmptyExtractionContext> weightModifier = null)
            : base(entryId, item, baseWeight, eligibility, weightModifier)
        {
        }
    }
}
