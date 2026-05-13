using System.Collections.Generic;
using UnityEngine;

public sealed class ContentPoolRollService
{
    private readonly IContentRandom random;
    private readonly List<ContentPoolCandidate> candidateBuffer = new();
    private readonly List<ContentRollItem> resultBuffer = new();
    private readonly List<ContentPoolEntry> selectedEntries = new();
    private readonly HashSet<string> selectedEntryIds = new(System.StringComparer.Ordinal);

    public ContentPoolRollService(IContentRandom random = null)
    {
        this.random = random ?? new UnityContentRandom();
    }

    public ContentRollResult Roll(
        ContentPoolSO pool,
        ContentRollContext rollContext,
        int? rollCountOverride = null,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        if (pool == null)
        {
            resultBuffer.Clear();
            return new ContentRollResult(resultBuffer);
        }

        ContentRollContext context = (rollContext ?? new ContentRollContext(ContentPoolScopeIds.Generic))
            .WithSelectedEntries(selectedEntries);
        return Roll(
            pool.Entries,
            context,
            rollCountOverride ?? pool.DefaultRollCount,
            pool.AllowDuplicateResults,
            entryFilter);
    }

    public ContentRollResult Roll(
        string scopeId,
        IReadOnlyList<ContentPoolEntry> entries,
        ContentRollContext rollContext,
        int rollCount = 1,
        bool allowDuplicateResults = false,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        ContentRollContext context = rollContext ?? new ContentRollContext(scopeId);
        return Roll(entries, context, rollCount, allowDuplicateResults, entryFilter);
    }

    private ContentRollResult Roll(
        IReadOnlyList<ContentPoolEntry> entries,
        ContentRollContext rollContext,
        int rollCount,
        bool allowDuplicateResults,
        System.Predicate<ContentPoolEntry> entryFilter)
    {
        resultBuffer.Clear();
        selectedEntries.Clear();
        selectedEntryIds.Clear();

        rollCount = Mathf.Max(1, rollCount);
        ContentRollContext contextWithSelection = (rollContext ?? new ContentRollContext(ContentPoolScopeIds.Generic))
            .WithSelectedEntries(selectedEntries);
        BuildCandidates(entries, contextWithSelection, entryFilter);
        ApplyModifiers(contextWithSelection);

        for (int i = 0; i < rollCount; i++)
        {
            ContentPoolCandidate selected = PickCandidate(allowDuplicateResults);
            if (selected == null)
            {
                break;
            }

            resultBuffer.Add(new ContentRollItem(selected));
            selectedEntries.Add(selected.Entry);
            if (!allowDuplicateResults)
            {
                selectedEntryIds.Add(selected.Entry.EntryId);
                selected.Remove();
            }

            RefreshCandidateAvailability(contextWithSelection);
        }

        ContentRollResult result = new(resultBuffer);
        contextWithSelection.RecordRoll(result);
        return result;
    }

    private void BuildCandidates(
        IReadOnlyList<ContentPoolEntry> entries,
        ContentRollContext context,
        System.Predicate<ContentPoolEntry> entryFilter)
    {
        candidateBuffer.Clear();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ContentPoolEntry entry = entries[i];
            if (entry == null || entry.Content == null || !CanUseEntry(context, entry))
            {
                continue;
            }

            if (entryFilter != null && !entryFilter(entry))
            {
                continue;
            }

            float weight = CalculateWeight(context, entry);
            if (weight <= 0f)
            {
                continue;
            }

            candidateBuffer.Add(new ContentPoolCandidate(entry, weight));
        }
    }

    private static bool CanUseEntry(ContentRollContext context, ContentPoolEntry entry)
    {
        if (entry.MaxRollCount > 0 && context.GetRollCount(entry.EntryId) >= entry.MaxRollCount)
        {
            return false;
        }

        if (entry.MaxPickCount > 0 && context.GetPickCount(entry.EntryId) >= entry.MaxPickCount)
        {
            return false;
        }

        IReadOnlyList<ContentCondition> conditions = entry.Conditions;
        if (conditions == null)
        {
            return true;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            ContentCondition condition = conditions[i];
            if (condition != null && !condition.IsSatisfied(context, entry))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SelectedEntriesAllowCandidate(ContentRollContext context, ContentPoolEntry candidateEntry)
    {
        if (context?.SelectedEntries == null || candidateEntry == null)
        {
            return true;
        }

        for (int i = 0; i < context.SelectedEntries.Count; i++)
        {
            ContentPoolEntry selectedEntry = context.SelectedEntries[i];
            if (selectedEntry == null)
            {
                continue;
            }

            if (EntriesAreMutuallyExclusive(selectedEntry, candidateEntry))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EntriesAreMutuallyExclusive(ContentPoolEntry selectedEntry, ContentPoolEntry candidateEntry)
    {
        if (selectedEntry == null || candidateEntry == null)
        {
            return false;
        }

        return ContainsEntryId(selectedEntry.MutuallyExclusiveEntryIds, candidateEntry.EntryId) ||
               ContainsEntryId(candidateEntry.MutuallyExclusiveEntryIds, selectedEntry.EntryId);
    }

    private static bool ContainsEntryId(IReadOnlyList<string> entryIds, string entryId)
    {
        if (entryIds == null || string.IsNullOrWhiteSpace(entryId))
        {
            return false;
        }

        for (int i = 0; i < entryIds.Count; i++)
        {
            if (string.Equals(entryIds[i], entryId, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static float CalculateWeight(ContentRollContext context, ContentPoolEntry entry)
    {
        float weight = entry.BaseWeight;
        IReadOnlyList<ContentWeightRule> rules = entry.WeightRules;
        if (rules == null)
        {
            return Mathf.Max(0f, weight);
        }

        for (int i = 0; i < rules.Count; i++)
        {
            ContentWeightRule rule = rules[i];
            if (rule != null)
            {
                weight = rule.ModifyWeight(weight, context, entry);
            }
        }

        return Mathf.Max(0f, weight);
    }

    private static void RefreshCandidateAvailability(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate == null || candidate.IsRemoved || candidate.Entry == null)
            {
                continue;
            }

            if (!CanUseEntry(context, candidate.Entry) ||
                !SelectedEntriesAllowCandidate(context, candidate.Entry))
            {
                candidate.Remove();
            }
        }
    }

    private void RefreshCandidateAvailability(ContentRollContext context)
    {
        RefreshCandidateAvailability(context, candidateBuffer);
    }

    private void ApplyModifiers(ContentRollContext context)
    {
        IReadOnlyList<IContentPoolModifier> modifiers = ContentPoolModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            IContentPoolModifier modifier = modifiers[i];
            if (modifier == null || !modifier.AffectsContext(context))
            {
                continue;
            }

            modifier.ModifyCandidates(context, candidateBuffer);
        }
    }

    private ContentPoolCandidate PickCandidate(bool allowDuplicateResults)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            ContentPoolCandidate candidate = candidateBuffer[i];
            if (!IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                continue;
            }

            totalWeight += Mathf.Max(0f, candidate.Weight);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float cursor = random.Value01() * totalWeight;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            ContentPoolCandidate candidate = candidateBuffer[i];
            if (!IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                continue;
            }

            cursor -= Mathf.Max(0f, candidate.Weight);
            if (cursor <= 0f)
            {
                return candidate;
            }
        }

        for (int i = candidateBuffer.Count - 1; i >= 0; i--)
        {
            ContentPoolCandidate candidate = candidateBuffer[i];
            if (IsCandidateAvailable(candidate, allowDuplicateResults))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsCandidateAvailable(ContentPoolCandidate candidate, bool allowDuplicateResults)
    {
        if (candidate == null || candidate.IsRemoved || candidate.Entry == null || candidate.Content == null)
        {
            return false;
        }

        if (!allowDuplicateResults && selectedEntryIds.Contains(candidate.Entry.EntryId))
        {
            return false;
        }

        return candidate.Weight > 0f;
    }
}
