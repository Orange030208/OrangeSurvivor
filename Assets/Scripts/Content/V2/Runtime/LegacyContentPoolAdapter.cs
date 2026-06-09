using System;
using System.Collections.Generic;
using UnityEngine;

public static class LegacyContentPoolAdapter
{
    public static ContentRollRequest CreateRequest(
        ContentPoolSO pool,
        ContentRollContext legacyContext,
        ContentRollScope scope,
        int? rollCountOverride = null,
        Predicate<ContentPoolEntryDefinition> entryFilter = null,
        RunContentHistory history = null,
        ContentFactSet facts = null,
        IContentRandom random = null)
    {
        ContentRollContext context = legacyContext ?? new ContentRollContext(scope.ScopeId);
        IReadOnlyList<ContentPoolEntryDefinition> entries = BuildEntries(pool, context);
        return new ContentRollRequest(
            entries,
            scope,
            rollCountOverride ?? (pool != null ? pool.DefaultRollCount : 1),
            pool != null && pool.AllowDuplicateResults,
            history,
            facts,
            entryFilter,
            random,
            context);
    }

    public static ContentRollRequest CreateRequest(
        string scopeId,
        IReadOnlyList<ContentPoolEntry> entries,
        ContentRollContext legacyContext,
        ContentRollScope scope,
        int rollCount,
        bool allowDuplicateResults,
        Predicate<ContentPoolEntryDefinition> entryFilter = null,
        RunContentHistory history = null,
        ContentFactSet facts = null,
        IContentRandom random = null)
    {
        ContentRollContext context = legacyContext ?? new ContentRollContext(scopeId);
        IReadOnlyList<ContentPoolEntryDefinition> definitions = BuildEntries(entries, context);
        return new ContentRollRequest(
            definitions,
            scope,
            rollCount,
            allowDuplicateResults,
            history,
            facts,
            entryFilter,
            random,
            context);
    }

    public static IReadOnlyList<ContentPoolEntryDefinition> BuildEntries(ContentPoolSO pool, ContentRollContext context)
    {
        if (pool == null || pool.Entries == null)
        {
            return Array.Empty<ContentPoolEntryDefinition>();
        }

        List<ContentPoolEntryDefinition> entries = new();
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            ContentPoolEntry entry = pool.Entries[i];
            if (entry == null || entry.Content == null || !CanUseLegacyEntry(context, entry))
            {
                continue;
            }

            float weight = CalculateLegacyWeight(context, entry);
            if (weight <= 0f)
            {
                continue;
            }

            ContentPoolEntryDefinition definition = ContentPoolEntryDefinition.FromLegacy(entry, weight);
            if (definition != null)
            {
                entries.Add(definition);
            }
        }

        return entries;
    }

    public static IReadOnlyList<ContentPoolEntryDefinition> BuildEntries(
        IReadOnlyList<ContentPoolEntry> sourceEntries,
        ContentRollContext context)
    {
        if (sourceEntries == null)
        {
            return Array.Empty<ContentPoolEntryDefinition>();
        }

        List<ContentPoolEntryDefinition> entries = new();
        for (int i = 0; i < sourceEntries.Count; i++)
        {
            ContentPoolEntry entry = sourceEntries[i];
            if (entry == null || entry.Content == null || !CanUseLegacyEntry(context, entry))
            {
                continue;
            }

            float weight = CalculateLegacyWeight(context, entry);
            if (weight <= 0f)
            {
                continue;
            }

            ContentPoolEntryDefinition definition = ContentPoolEntryDefinition.FromLegacy(entry, weight);
            if (definition != null)
            {
                entries.Add(definition);
            }
        }

        return entries;
    }

    private static bool CanUseLegacyEntry(ContentRollContext context, ContentPoolEntry entry)
    {
        if (context != null)
        {
            if (entry.MaxRollCount > 0 && context.GetRollCount(entry.EntryId) >= entry.MaxRollCount)
            {
                return false;
            }

            if (entry.MaxPickCount > 0 && context.GetPickCount(entry.EntryId) >= entry.MaxPickCount)
            {
                return false;
            }
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

    private static float CalculateLegacyWeight(ContentRollContext context, ContentPoolEntry entry)
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
}

public sealed class LegacyGlobalContentModifierSource : IContentModifierSource
{
    public static LegacyGlobalContentModifierSource Instance { get; } = new();

    private LegacyGlobalContentModifierSource()
    {
    }

    public void ModifyCandidates(ContentRollRequest request, List<ContentRollCandidate> candidates)
    {
        if (request?.LegacyContext == null || candidates == null || candidates.Count == 0)
        {
            return;
        }

        IReadOnlyList<IContentPoolModifier> modifiers = ContentPoolModifierRegistry.ActiveModifiers;
        if (modifiers.Count == 0)
        {
            return;
        }

        List<ContentPoolCandidate> legacyCandidates = new(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolEntry legacyEntry = candidates[i].Entry?.LegacyEntry;
            if (legacyEntry == null)
            {
                legacyCandidates.Add(null);
                continue;
            }

            ContentPoolCandidate legacyCandidate = new(legacyEntry, candidates[i].Weight);
            legacyCandidate.ReplaceMetadataForV2(candidates[i].Metadata);
            legacyCandidates.Add(legacyCandidate);
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            IContentPoolModifier modifier = modifiers[i];
            if (modifier == null || !modifier.AffectsContext(request.LegacyContext))
            {
                continue;
            }

            modifier.ModifyCandidates(request.LegacyContext, legacyCandidates);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate legacyCandidate = legacyCandidates[i];
            if (legacyCandidate == null)
            {
                continue;
            }

            candidates[i].Weight = legacyCandidate.Weight;
            candidates[i].ReplaceMetadata(legacyCandidate.Metadata);
            if (legacyCandidate.IsRemoved)
            {
                candidates[i].Remove();
            }
        }
    }
}
