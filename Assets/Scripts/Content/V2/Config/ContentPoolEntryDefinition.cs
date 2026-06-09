using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ContentPoolEntryDefinition
{
    [SerializeField] private string entryId;
    [SerializeField] private UnityEngine.Object content;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField, Min(0)] private int maxRollCount;
    [SerializeField, Min(0)] private int maxPickCount;
    [SerializeField] private List<string> mutuallyExclusiveEntryIds = new();
    [SerializeReference] private List<ContentEntryMetadata> metadata = new();

    [NonSerialized] private ContentPoolEntry legacyEntry;

    public string EntryId => string.IsNullOrWhiteSpace(entryId)
        ? content != null ? content.name : string.Empty
        : entryId;
    public UnityEngine.Object Content => content;
    public float BaseWeight => Mathf.Max(0f, baseWeight);
    public int MaxRollCount => Mathf.Max(0, maxRollCount);
    public int MaxPickCount => Mathf.Max(0, maxPickCount);
    public IReadOnlyList<string> MutuallyExclusiveEntryIds => mutuallyExclusiveEntryIds != null
        ? mutuallyExclusiveEntryIds
        : Array.Empty<string>();
    public IReadOnlyList<ContentEntryMetadata> Metadata => metadata != null
        ? metadata
        : Array.Empty<ContentEntryMetadata>();
    public ContentPoolEntry LegacyEntry => legacyEntry;

    public ContentPoolEntryDefinition()
    {
    }

    public ContentPoolEntryDefinition(
        UnityEngine.Object content,
        float baseWeight,
        string entryId = null,
        IReadOnlyList<ContentEntryMetadata> metadata = null,
        int maxRollCount = 0,
        int maxPickCount = 0,
        IReadOnlyList<string> mutuallyExclusiveEntryIds = null,
        ContentPoolEntry legacyEntry = null)
    {
        this.content = content;
        this.baseWeight = Mathf.Max(0f, baseWeight);
        this.entryId = string.IsNullOrWhiteSpace(entryId) ? content != null ? content.name : string.Empty : entryId;
        this.maxRollCount = Mathf.Max(0, maxRollCount);
        this.maxPickCount = Mathf.Max(0, maxPickCount);
        this.mutuallyExclusiveEntryIds = mutuallyExclusiveEntryIds != null
            ? new List<string>(mutuallyExclusiveEntryIds)
            : new List<string>();
        this.metadata = ContentMetadataUtility.CloneMetadata(metadata);
        this.legacyEntry = legacyEntry;
    }

    public static ContentPoolEntryDefinition FromLegacy(ContentPoolEntry entry, float weight, IReadOnlyList<ContentEntryMetadata> metadata = null)
    {
        if (entry == null)
        {
            return null;
        }

        return new ContentPoolEntryDefinition(
            entry.Content,
            weight,
            entry.EntryId,
            metadata ?? entry.Metadata,
            entry.MaxRollCount,
            entry.MaxPickCount,
            entry.MutuallyExclusiveEntryIds,
            entry);
    }
}
