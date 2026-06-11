using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Orange.Extraction
{
    /// <summary>
    /// Pure C# weighted extraction pool that keeps item data, eligibility, and weight calculation explicit.
    /// </summary>
    public class WeightedExtractionPool<TItem, TContext>
    {
        private readonly List<WeightedExtractionEntry<TItem, TContext>> entries = new List<WeightedExtractionEntry<TItem, TContext>>();
        private readonly IExtractionRandom random;

        public WeightedExtractionPool()
            : this((IExtractionRandom)null)
        {
        }

        public WeightedExtractionPool(IExtractionRandom random)
        {
            this.random = random ?? new SystemExtractionRandom();
        }

        public WeightedExtractionPool(IEnumerable<TItem> items, float baseWeight = 1f, IExtractionRandom random = null)
            : this(random)
        {
            AddUniformEntries(items, baseWeight);
        }

        public WeightedExtractionPool(IEnumerable<WeightedExtractionEntry<TItem, TContext>> entries, IExtractionRandom random = null)
            : this(random)
        {
            AddEntries(entries);
        }

        public IReadOnlyList<WeightedExtractionEntry<TItem, TContext>> Entries => new ReadOnlyCollection<WeightedExtractionEntry<TItem, TContext>>(entries);
        public int Count => entries.Count;
        protected IExtractionRandom Random => random;

        public WeightedExtractionEntry<TItem, TContext> AddEntry(
            string entryId,
            TItem item,
            float baseWeight,
            ExtractionEligibility<TItem, TContext> eligibility = null,
            IExtractionWeightModifier<TItem, TContext> weightModifier = null)
        {
            WeightedExtractionEntry<TItem, TContext> entry = new WeightedExtractionEntry<TItem, TContext>(
                entryId,
                item,
                baseWeight,
                eligibility,
                weightModifier);

            AddEntry(entry);
            return entry;
        }

        public void AddEntry(WeightedExtractionEntry<TItem, TContext> entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            entries.Add(entry);
        }

        public void AddEntries(IEnumerable<WeightedExtractionEntry<TItem, TContext>> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            foreach (WeightedExtractionEntry<TItem, TContext> entry in entries)
            {
                AddEntry(entry);
            }
        }

        public void AddUniformEntries(IEnumerable<TItem> items, float baseWeight = 1f)
        {
            AddUniformEntries(items, null, baseWeight);
        }

        public void AddUniformEntries(IEnumerable<TItem> items, Func<TItem, string> entryIdSelector, float baseWeight = 1f)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            ExtractionValidation.ThrowIfInvalidBaseWeight(baseWeight, "uniform entries");

            int index = entries.Count;
            foreach (TItem item in items)
            {
                string entryId = entryIdSelector != null ? entryIdSelector(item) : CreateDefaultEntryId(item, index);
                AddEntry(entryId, item, baseWeight);
                index++;
            }
        }

        public void Clear()
        {
            entries.Clear();
        }

        public ExtractionEvaluation<TItem> Evaluate(TContext context)
        {
            return EvaluateEntries(context, null);
        }

        public bool TryDrawOne(TContext context, out ExtractionResult<TItem> result)
        {
            ExtractionEvaluation<TItem> evaluation = Evaluate(context);
            ExtractionCandidate<TItem> selectedCandidate;
            return TryDrawFromEvaluation(evaluation, out result, out selectedCandidate);
        }

        public IReadOnlyList<ExtractionResult<TItem>> DrawManyUnique(TContext context, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Extraction draw count cannot be negative.");
            }

            if (count == 0 || entries.Count == 0)
            {
                return Array.Empty<ExtractionResult<TItem>>();
            }

            List<ExtractionResult<TItem>> results = new List<ExtractionResult<TItem>>(Math.Min(count, entries.Count));
            HashSet<int> selectedEntryIndices = new HashSet<int>();

            for (int i = 0; i < count; i++)
            {
                // Reevaluate remaining entries each roll so context-sensitive modifiers stay authoritative.
                ExtractionEvaluation<TItem> evaluation = EvaluateEntries(context, selectedEntryIndices);
                ExtractionResult<TItem> result;
                ExtractionCandidate<TItem> selectedCandidate;

                if (!TryDrawFromEvaluation(evaluation, out result, out selectedCandidate))
                {
                    break;
                }

                results.Add(result);
                selectedEntryIndices.Add(selectedCandidate.EntryIndex);
            }

            return new ReadOnlyCollection<ExtractionResult<TItem>>(results);
        }

        protected virtual string CreateDefaultEntryId(TItem item, int index)
        {
            string itemName = item != null ? item.ToString() : null;
            return string.IsNullOrWhiteSpace(itemName) ? $"entry_{index}" : itemName;
        }

        private ExtractionEvaluation<TItem> EvaluateEntries(TContext context, ISet<int> excludedEntryIndices)
        {
            List<ExtractionCandidate<TItem>> candidates = new List<ExtractionCandidate<TItem>>();
            double totalWeight = 0d;

            for (int i = 0; i < entries.Count; i++)
            {
                if (excludedEntryIndices != null && excludedEntryIndices.Contains(i))
                {
                    continue;
                }

                WeightedExtractionEntry<TItem, TContext> entry = entries[i];
                bool isEligible = entry.IsEligible(context);
                float finalWeight = 0f;
                ExtractionCandidateStatus status = ExtractionCandidateStatus.Ineligible;

                if (isEligible)
                {
                    finalWeight = CalculateFinalWeight(entry, context);
                    status = finalWeight > 0f ? ExtractionCandidateStatus.Drawable : ExtractionCandidateStatus.ZeroWeight;

                    if (status == ExtractionCandidateStatus.Drawable)
                    {
                        totalWeight += finalWeight;
                    }
                }

                candidates.Add(new ExtractionCandidate<TItem>(
                    i,
                    entry.EntryId,
                    entry.Item,
                    entry.BaseWeight,
                    finalWeight,
                    status));
            }

            return new ExtractionEvaluation<TItem>(candidates, totalWeight);
        }

        private float CalculateFinalWeight(WeightedExtractionEntry<TItem, TContext> entry, TContext context)
        {
            float finalWeight = entry.BaseWeight;

            if (entry.WeightModifier != null)
            {
                finalWeight = entry.WeightModifier.ModifyWeight(entry, entry.BaseWeight, context);
            }

            ExtractionValidation.ThrowIfInvalidFinalWeight(finalWeight, entry.EntryId);
            return finalWeight < 0f ? 0f : finalWeight;
        }

        private bool TryDrawFromEvaluation(
            ExtractionEvaluation<TItem> evaluation,
            out ExtractionResult<TItem> result,
            out ExtractionCandidate<TItem> selectedCandidate)
        {
            result = null;
            selectedCandidate = null;

            if (evaluation == null)
            {
                throw new ArgumentNullException(nameof(evaluation));
            }

            if (evaluation.TotalWeight <= 0d)
            {
                return false;
            }

            float normalizedRandomValue = random.NextNormalizedValue();
            ExtractionValidation.ThrowIfInvalidRandomValue(normalizedRandomValue);

            double rollValue = normalizedRandomValue * evaluation.TotalWeight;
            selectedCandidate = SelectCandidate(evaluation, rollValue);

            if (selectedCandidate == null)
            {
                return false;
            }

            result = new ExtractionResult<TItem>(
                selectedCandidate,
                evaluation.TotalWeight,
                rollValue,
                evaluation);

            return true;
        }

        private static ExtractionCandidate<TItem> SelectCandidate(ExtractionEvaluation<TItem> evaluation, double rollValue)
        {
            double remainingRoll = rollValue;
            ExtractionCandidate<TItem> lastDrawableCandidate = null;

            for (int i = 0; i < evaluation.Candidates.Count; i++)
            {
                ExtractionCandidate<TItem> candidate = evaluation.Candidates[i];
                if (!candidate.IsDrawable)
                {
                    continue;
                }

                lastDrawableCandidate = candidate;

                if (remainingRoll < candidate.FinalWeight)
                {
                    return candidate;
                }

                remainingRoll -= candidate.FinalWeight;
            }

            return lastDrawableCandidate;
        }
    }

    public class WeightedExtractionPool<TItem> : WeightedExtractionPool<TItem, EmptyExtractionContext>
    {
        public WeightedExtractionPool()
        {
        }

        public WeightedExtractionPool(IExtractionRandom random)
            : base(random)
        {
        }

        public WeightedExtractionPool(IEnumerable<TItem> items, float baseWeight = 1f, IExtractionRandom random = null)
            : base(items, baseWeight, random)
        {
        }

        public WeightedExtractionPool(IEnumerable<WeightedExtractionEntry<TItem, EmptyExtractionContext>> entries, IExtractionRandom random = null)
            : base(entries, random)
        {
        }

        public ExtractionEvaluation<TItem> Evaluate()
        {
            return Evaluate(EmptyExtractionContext.Default);
        }

        public bool TryDrawOne(out ExtractionResult<TItem> result)
        {
            return TryDrawOne(EmptyExtractionContext.Default, out result);
        }

        public IReadOnlyList<ExtractionResult<TItem>> DrawManyUnique(int count)
        {
            return DrawManyUnique(EmptyExtractionContext.Default, count);
        }
    }
}
