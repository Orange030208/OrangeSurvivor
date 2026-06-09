using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ContentRollRequest
{
    public ContentRollRequest(
        IReadOnlyList<ContentPoolEntryDefinition> entries,
        ContentRollScope scope,
        int rollCount,
        bool allowDuplicateResults,
        RunContentHistory history = null,
        ContentFactSet facts = null,
        Predicate<ContentPoolEntryDefinition> entryFilter = null,
        IContentRandom random = null,
        ContentRollContext legacyContext = null)
    {
        Entries = entries ?? Array.Empty<ContentPoolEntryDefinition>();
        Scope = scope;
        RollCount = Mathf.Max(1, rollCount);
        AllowDuplicateResults = allowDuplicateResults;
        History = history;
        Facts = facts ?? ContentFactSet.Empty;
        EntryFilter = entryFilter;
        Random = random;
        LegacyContext = legacyContext;
    }

    public IReadOnlyList<ContentPoolEntryDefinition> Entries { get; }
    public ContentRollScope Scope { get; }
    public int RollCount { get; }
    public bool AllowDuplicateResults { get; }
    public RunContentHistory History { get; }
    public ContentFactSet Facts { get; }
    public Predicate<ContentPoolEntryDefinition> EntryFilter { get; }
    public IContentRandom Random { get; }
    public ContentRollContext LegacyContext { get; }

    public static ContentRollRequest FromProfile(
        ContentPoolProfileSO profile,
        ContentRollScope? scope = null,
        int? rollCountOverride = null,
        RunContentHistory history = null,
        ContentFactSet facts = null,
        Predicate<ContentPoolEntryDefinition> entryFilter = null,
        IContentRandom random = null)
    {
        if (profile == null)
        {
            ContentRollScope emptyScope = scope ?? ContentRollScope.FromKind(ContentPoolKind.Generic);
            return new ContentRollRequest(
                Array.Empty<ContentPoolEntryDefinition>(),
                emptyScope,
                rollCountOverride ?? 1,
                false,
                history,
                facts,
                entryFilter,
                random);
        }

        ContentRollScope resolvedScope = scope ??
            ContentRollScope.FromKind(profile.Kind, profile.PoolId);
        return new ContentRollRequest(
            profile.Entries,
            resolvedScope,
            rollCountOverride ?? profile.DefaultRollCount,
            profile.AllowDuplicateResults,
            history,
            facts,
            entryFilter,
            random);
    }
}

public sealed class ContentRollOutcome
{
    private readonly List<ContentRollSelection> selections;

    public ContentRollOutcome(IReadOnlyList<ContentRollSelection> selections)
    {
        this.selections = selections != null
            ? new List<ContentRollSelection>(selections)
            : new List<ContentRollSelection>();
    }

    public IReadOnlyList<ContentRollSelection> Selections => selections;
    public bool HasAny => selections.Count > 0;

    public ContentRollResult ToLegacyResult()
    {
        List<ContentRollItem> items = new(selections.Count);
        for (int i = 0; i < selections.Count; i++)
        {
            items.Add(selections[i].ToLegacyItem());
        }

        return new ContentRollResult(items);
    }
}

public readonly struct ContentRollSelection : IHasContentTier
{
    private readonly IReadOnlyList<ContentEntryMetadata> metadata;

    public ContentRollSelection(
        ContentPoolEntryDefinition entry,
        UnityEngine.Object content,
        float finalWeight,
        IReadOnlyList<ContentEntryMetadata> metadata)
    {
        Entry = entry;
        Content = content;
        FinalWeight = finalWeight;
        this.metadata = ContentMetadataUtility.CloneMetadata(metadata);
    }

    public ContentPoolEntryDefinition Entry { get; }
    public UnityEngine.Object Content { get; }
    public float FinalWeight { get; }
    public string EntryId => Entry != null ? Entry.EntryId : Content != null ? Content.name : string.Empty;
    public IReadOnlyList<ContentEntryMetadata> Metadata => metadata ?? Array.Empty<ContentEntryMetadata>();
    public ContentTier Tier => TryGetTier(out ContentTier tier) ? tier : ContentTier.Common;

    public bool TryGetMetadata<T>(out T value)
        where T : ContentEntryMetadata
    {
        return ContentMetadataUtility.TryGetMetadata(Metadata, out value);
    }

    public bool TryGetTier(out ContentTier tier)
    {
        if (TryGetMetadata(out QualityMetadata qualityMetadata))
        {
            tier = qualityMetadata.Tier;
            return true;
        }

        tier = default;
        return false;
    }

    public ContentRollItem ToLegacyItem()
    {
        return new ContentRollItem(
            Entry != null ? Entry.LegacyEntry : null,
            Content,
            FinalWeight,
            Metadata);
    }
}
