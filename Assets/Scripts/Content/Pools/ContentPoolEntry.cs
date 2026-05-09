using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ContentPoolEntry
{
    private const float MIN_PRICE_MULTIPLIER = 0.01f;

    [SerializeField] private string entryId;
    [SerializeField] private UnityEngine.Object content;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField] private List<ContentTagSO> tags = new();
    [SerializeField, Min(0)] private int maxRollCount;
    [SerializeField, Min(0)] private int maxPickCount;
    [SerializeField] private List<string> mutuallyExclusiveEntryIds = new();

    [Header("Domain Metadata")]
    [SerializeField, Min(0)] private int minLevel;
    [SerializeField, Min(0)] private int maxLevel;
    [SerializeField] private int qualityValue;
    [SerializeField] private int domainFlags;
    [SerializeField, Min(MIN_PRICE_MULTIPLIER)] private float priceMultiplier = 1f;

    [Header("Rules")]
    [SerializeReference] private List<ContentCondition> conditions = new();
    [SerializeReference] private List<ContentWeightRule> weightRules = new();

    public string EntryId => string.IsNullOrWhiteSpace(entryId)
        ? content != null ? content.name : string.Empty
        : entryId;
    public UnityEngine.Object Content => content;
    public float BaseWeight => Mathf.Max(0f, baseWeight);
    public IReadOnlyList<ContentTagSO> Tags => tags;
    public int MaxRollCount => Mathf.Max(0, maxRollCount);
    public int MaxPickCount => Mathf.Max(0, maxPickCount);
    public IReadOnlyList<string> MutuallyExclusiveEntryIds => mutuallyExclusiveEntryIds;
    public int MinLevel => Mathf.Max(0, minLevel);
    public int MaxLevel => Mathf.Max(MinLevel, maxLevel);
    public int QualityValue => qualityValue;
    public int DomainFlags => domainFlags;
    public float PriceMultiplier => Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier);
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
        tags = new List<ContentTagSO>();
        mutuallyExclusiveEntryIds = new List<string>();
        conditions = new List<ContentCondition>();
        weightRules = new List<ContentWeightRule>();
        priceMultiplier = 1f;
    }

    public void ConfigureRuntimeLimits(
        int maxRollCount,
        int maxPickCount,
        IReadOnlyList<string> mutuallyExclusiveEntryIds)
    {
        this.maxRollCount = Mathf.Max(0, maxRollCount);
        this.maxPickCount = Mathf.Max(0, maxPickCount);
        this.mutuallyExclusiveEntryIds = mutuallyExclusiveEntryIds != null
            ? new List<string>(mutuallyExclusiveEntryIds)
            : new List<string>();
    }

    public void ConfigureRuntimeMetadata(
        int minLevel,
        int maxLevel,
        int qualityValue,
        float priceMultiplier,
        int domainFlags = 0)
    {
        this.minLevel = Mathf.Max(0, minLevel);
        this.maxLevel = Mathf.Max(this.minLevel, maxLevel);
        this.qualityValue = qualityValue;
        this.domainFlags = domainFlags;
        this.priceMultiplier = Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier);
    }

    public void ConfigureRuntimeTags(IReadOnlyList<ContentTagSO> runtimeTags)
    {
        tags = runtimeTags != null
            ? new List<ContentTagSO>(runtimeTags)
            : new List<ContentTagSO>();
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

    public bool HasTag(ContentTagSO tag)
    {
        if (tag == null || tags == null)
        {
            return false;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            if (tags[i] == tag)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsMutuallyExclusiveWith(string otherEntryId)
    {
        if (string.IsNullOrWhiteSpace(otherEntryId) || mutuallyExclusiveEntryIds == null)
        {
            return false;
        }

        for (int i = 0; i < mutuallyExclusiveEntryIds.Count; i++)
        {
            if (string.Equals(mutuallyExclusiveEntryIds[i], otherEntryId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        CollectFactDefinitions(conditions, results);
        CollectFactDefinitions(weightRules, results);
    }

    private static void CollectFactDefinitions<T>(IReadOnlyList<T> rules, List<FactDefinitionSO> results)
        where T : class, IContentFactDefinitionProvider
    {
        if (rules == null)
        {
            return;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            rules[i]?.CollectFactDefinitions(results);
        }
    }
}
