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
        ContentFactSet facts,
        ContentPoolRuntimeState runtimeState = null,
        int? rollCountOverride = null,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        if (pool == null)
        {
            resultBuffer.Clear();
            return new ContentRollResult(resultBuffer);
        }

        return Roll(
            pool.Purpose,
            pool.Entries,
            facts,
            runtimeState,
            rollCountOverride ?? pool.DefaultRollCount,
            pool.AllowDuplicateResults,
            entryFilter);
    }

    public ContentRollResult Roll(
        ContentPoolSO pool,
        ContentFactSource factSource,
        ContentPoolRuntimeState runtimeState = null,
        int? rollCountOverride = null,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        List<FactDefinitionSO> definitions = new();
        pool?.CollectFactDefinitions(definitions);
        CollectModifierFactDefinitions(pool != null ? pool.Purpose : ContentPoolPurpose.Generic, definitions);
        ContentFactSet facts = ContentFactCollector.Collect(factSource, definitions);
        return Roll(pool, facts, runtimeState, rollCountOverride, entryFilter);
    }

    public ContentRollResult Roll(
        ContentPoolPurpose purpose,
        IReadOnlyList<ContentPoolEntry> entries,
        ContentFactSource factSource,
        ContentPoolRuntimeState runtimeState = null,
        int rollCount = 1,
        bool allowDuplicateResults = false,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        List<FactDefinitionSO> definitions = new();
        CollectFactDefinitions(entries, definitions);
        CollectModifierFactDefinitions(purpose, definitions);
        ContentFactSet facts = ContentFactCollector.Collect(factSource, definitions);
        return Roll(purpose, entries, facts, runtimeState, rollCount, allowDuplicateResults, entryFilter);
    }

    public ContentRollResult Roll(
        ContentPoolPurpose purpose,
        IReadOnlyList<ContentPoolEntry> entries,
        ContentFactSet facts,
        ContentPoolRuntimeState runtimeState = null,
        int rollCount = 1,
        bool allowDuplicateResults = false,
        System.Predicate<ContentPoolEntry> entryFilter = null)
    {
        resultBuffer.Clear();
        selectedEntries.Clear();
        selectedEntryIds.Clear();

        rollCount = Mathf.Max(1, rollCount);
        ContentPoolEvaluationContext context = new(purpose, facts, runtimeState);
        BuildCandidates(entries, context, entryFilter);
        ApplyModifiers(context);

        for (int i = 0; i < rollCount; i++)
        {
            ContentPoolCandidate selected = PickCandidate(allowDuplicateResults);
            if (selected == null)
            {
                break;
            }

            resultBuffer.Add(new ContentRollItem(selected));
            if (!allowDuplicateResults)
            {
                selectedEntries.Add(selected.Entry);
                selectedEntryIds.Add(selected.Entry.EntryId);
                selected.Remove();
            }
        }

        ContentRollResult result = new(resultBuffer);
        runtimeState?.RecordRoll(result.Items);
        return result;
    }

    private void BuildCandidates(
        IReadOnlyList<ContentPoolEntry> entries,
        ContentPoolEvaluationContext context,
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

    private static bool CanUseEntry(ContentPoolEvaluationContext context, ContentPoolEntry entry)
    {
        if (entry.MaxRollCount > 0 && context.RuntimeState != null &&
            context.RuntimeState.GetRollCount(entry.EntryId) >= entry.MaxRollCount)
        {
            return false;
        }

        if (entry.MaxPickCount > 0 && context.RuntimeState != null &&
            context.RuntimeState.GetPickCount(entry.EntryId) >= entry.MaxPickCount)
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

    private static float CalculateWeight(ContentPoolEvaluationContext context, ContentPoolEntry entry)
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

    private void ApplyModifiers(ContentPoolEvaluationContext context)
    {
        IReadOnlyList<IContentPoolModifier> modifiers = ContentPoolModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            IContentPoolModifier modifier = modifiers[i];
            if (modifier == null || !modifier.AffectsPurpose(context.Purpose))
            {
                continue;
            }

            modifier.ModifyCandidates(context, candidateBuffer);
        }
    }

    private static void CollectFactDefinitions(
        IReadOnlyList<ContentPoolEntry> entries,
        List<FactDefinitionSO> results)
    {
        if (entries == null || results == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            entries[i]?.CollectFactDefinitions(results);
        }
    }

    private static void CollectModifierFactDefinitions(ContentPoolPurpose purpose, List<FactDefinitionSO> results)
    {
        if (results == null)
        {
            return;
        }

        IReadOnlyList<IContentPoolModifier> modifiers = ContentPoolModifierRegistry.ActiveModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] is not IContentFactDefinitionProvider provider ||
                !modifiers[i].AffectsPurpose(purpose))
            {
                continue;
            }

            provider.CollectFactDefinitions(results);
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

        if (!allowDuplicateResults && IsMutuallyExclusiveWithSelected(candidate.Entry))
        {
            return false;
        }

        return candidate.Weight > 0f;
    }

    private bool IsMutuallyExclusiveWithSelected(ContentPoolEntry entry)
    {
        if (entry == null || selectedEntryIds.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < selectedEntries.Count; i++)
        {
            ContentPoolEntry selectedEntry = selectedEntries[i];
            if (selectedEntry == null)
            {
                continue;
            }

            if (entry.IsMutuallyExclusiveWith(selectedEntry.EntryId) ||
                selectedEntry.IsMutuallyExclusiveWith(entry.EntryId))
            {
                return true;
            }
        }

        return false;
    }
}
