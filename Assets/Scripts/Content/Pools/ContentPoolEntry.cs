using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ContentPoolEntry
{
    [SerializeField] private string entryId;
    [SerializeField] private UnityEngine.Object content;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField, Min(0)] private int maxRollCount;
    [SerializeField, Min(0)] private int maxPickCount;
    [SerializeField] private List<string> mutuallyExclusiveEntryIds = new();

    [Header("领域元数据")]
    [SerializeReference] private List<ContentEntryMetadata> metadata = new();

    [Header("规则")]
    [SerializeReference] private List<ContentCondition> conditions = new();
    [SerializeReference] private List<ContentWeightRule> weightRules = new();

    public string EntryId => string.IsNullOrWhiteSpace(entryId)
        ? content != null ? content.name : string.Empty
        : entryId;
    public UnityEngine.Object Content => content;
    public float BaseWeight => Mathf.Max(0f, baseWeight);
    public int MaxRollCount => Mathf.Max(0, maxRollCount);
    public int MaxPickCount => Mathf.Max(0, maxPickCount);
    public IReadOnlyList<string> MutuallyExclusiveEntryIds => mutuallyExclusiveEntryIds;
    public IReadOnlyList<ContentEntryMetadata> Metadata => metadata;
    public IReadOnlyList<ContentCondition> Conditions => conditions;
    public IReadOnlyList<ContentWeightRule> WeightRules => weightRules;

    public ContentPoolEntry()
    {
    }

    public ContentPoolEntry(UnityEngine.Object content, float baseWeight, string entryId = null)
    {
        this.content = content;
        this.baseWeight = Mathf.Max(0f, baseWeight);
        this.entryId = string.IsNullOrWhiteSpace(entryId) ? content != null ? content.name : string.Empty : entryId;
        conditions = new List<ContentCondition>();
        weightRules = new List<ContentWeightRule>();
        mutuallyExclusiveEntryIds = new List<string>();
        metadata = new List<ContentEntryMetadata>();
    }

    public bool TryGetMetadata<T>(out T value)
        where T : ContentEntryMetadata
    {
        return ContentMetadataUtility.TryGetMetadata(metadata, out value);
    }

    public void ConfigureRuntimeLimits(int maxRollCount, int maxPickCount)
    {
        this.maxRollCount = Mathf.Max(0, maxRollCount);
        this.maxPickCount = Mathf.Max(0, maxPickCount);
    }

    public void ConfigureRuntimeMutuallyExclusiveEntries(IReadOnlyList<string> entryIds)
    {
        mutuallyExclusiveEntryIds = entryIds != null
            ? new List<string>(entryIds)
            : new List<string>();
    }

    public void ConfigureRuntimeMetadata(IReadOnlyList<ContentEntryMetadata> runtimeMetadata)
    {
        metadata = ContentMetadataUtility.CloneMetadata(runtimeMetadata);
    }

    public void ConfigureRuntimeRules(
        IReadOnlyList<ContentCondition> runtimeConditions,
        IReadOnlyList<ContentWeightRule> runtimeWeightRules)
    {
        conditions = runtimeConditions != null
            ? new List<ContentCondition>(runtimeConditions)
            : new List<ContentCondition>();
        weightRules = runtimeWeightRules != null
            ? new List<ContentWeightRule>(runtimeWeightRules)
            : new List<ContentWeightRule>();
    }
}
