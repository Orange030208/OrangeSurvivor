using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public readonly struct DropSourceInfo
{
    public DropSourceInfo(string sourceType, string sourceId)
    {
        SourceType = Normalize(sourceType);
        SourceId = Normalize(sourceId);
    }

    public string SourceType { get; }
    public string SourceId { get; }

    public static DropSourceInfo FromEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return new DropSourceInfo(string.Empty, string.Empty);
        }

        return new DropSourceInfo(
            enemy.Role.ToString(),
            enemy.EnemyData != null ? enemy.EnemyData.name : enemy.name);
    }

    public static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public readonly struct DropRollResult
{
    public DropRollResult(CollectionSO collection, int quantity = 1)
    {
        Collection = collection;
        Quantity = Mathf.Max(1, quantity);
    }

    public static DropRollResult None => new(null, 1);

    public CollectionSO Collection { get; }
    public int Quantity { get; }
}

[Serializable]
public sealed class DropSourceRuleData
{
    public const float LUCK_WEIGHT_DIVISOR = 250f;
    public const float LUCK_DROP_CHANCE_PER_POINT = 0.004f;

    [Header("来源")]
    [SerializeField] private string sourceType;
    [SerializeField] private string sourceId;

    [Header("击杀收益")]
    [SerializeField, Min(0)] private int killExperience = 1;
    [SerializeField, Range(0f, 1f)] private float dropChance;
    [SerializeField, Range(0f, 1f)] private float dropChanceCap = 1f;

    [Header("产物")]
    [SerializeField] private List<DropProductRuleData> products = new();

    public DropSourceRuleData()
    {
    }

    public DropSourceRuleData(
        string sourceType,
        string sourceId,
        int killExperience,
        float dropChance,
        float dropChanceCap,
        IReadOnlyList<DropProductRuleData> products)
    {
        this.sourceType = sourceType;
        this.sourceId = sourceId;
        this.killExperience = Mathf.Max(0, killExperience);
        this.dropChance = Mathf.Clamp01(dropChance);
        this.dropChanceCap = Mathf.Clamp01(dropChanceCap);
        this.products = products != null
            ? new List<DropProductRuleData>(products)
            : new List<DropProductRuleData>();
    }

    public string SourceType => DropSourceInfo.Normalize(sourceType);
    public string SourceId => DropSourceInfo.Normalize(sourceId);
    public int KillExperience => Mathf.Max(0, killExperience);
    public float DropChance => Mathf.Clamp01(dropChance);
    public float DropChanceCap => Mathf.Clamp01(dropChanceCap);
    public IReadOnlyList<DropProductRuleData> Products => products != null
        ? products
        : Array.Empty<DropProductRuleData>();
    public bool HasProductRules => products != null && products.Count > 0;

    public bool Matches(DropSourceInfo sourceInfo)
    {
        return MatchesValue(SourceType, sourceInfo.SourceType) &&
               MatchesValue(SourceId, sourceInfo.SourceId);
    }

    public int GetMatchScore(DropSourceInfo sourceInfo)
    {
        if (!Matches(sourceInfo))
        {
            return -1;
        }

        int score = 0;
        if (!string.IsNullOrWhiteSpace(SourceType))
        {
            score++;
        }

        if (!string.IsNullOrWhiteSpace(SourceId))
        {
            score += 2;
        }

        return score;
    }

    public float EvaluateDropChance(float luck)
    {
        float multiplier = 1f + luck * LUCK_DROP_CHANCE_PER_POINT;
        float scaledChance = DropChance * Mathf.Max(0f, multiplier);
        return Mathf.Clamp(scaledChance, 0f, DropChanceCap);
    }

    private static bool MatchesValue(string ruleValue, string sourceValue)
    {
        return string.IsNullOrWhiteSpace(ruleValue) ||
               string.Equals(ruleValue, sourceValue, StringComparison.OrdinalIgnoreCase);
    }
}

[Serializable]
public sealed class DropProductRuleData
{
    [SerializeField] private ContentPoolSO productPool;
    [SerializeField] private CollectionSO product;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField] private float luckCoefficient;
    [SerializeField, Min(1)] private int quantity = 1;

    public DropProductRuleData()
    {
    }

    public DropProductRuleData(
        CollectionSO product,
        float baseWeight,
        float luckCoefficient = 0f,
        int quantity = 1)
    {
        this.product = product;
        this.baseWeight = Mathf.Max(0f, baseWeight);
        this.luckCoefficient = luckCoefficient;
        this.quantity = Mathf.Max(1, quantity);
    }

    public DropProductRuleData(
        ContentPoolSO productPool,
        float baseWeight,
        float luckCoefficient = 0f,
        int quantity = 1)
    {
        this.productPool = productPool;
        this.baseWeight = Mathf.Max(0f, baseWeight);
        this.luckCoefficient = luckCoefficient;
        this.quantity = Mathf.Max(1, quantity);
    }

    public ContentPoolSO ProductPool => productPool;
    public CollectionSO Product => product;
    public float BaseWeight => Mathf.Max(0f, baseWeight);
    public float LuckCoefficient => luckCoefficient;
    public int Quantity => Mathf.Max(1, quantity);

    public ContentPoolEntry CreateEntry(ContentPoolSO fallbackPool, int index)
    {
        UnityEngine.Object content = ResolveContent(fallbackPool);
        if (content == null || BaseWeight <= 0f)
        {
            return null;
        }

        ContentPoolEntry entry = new(content, BaseWeight, ResolveEntryId(content, index));
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new DropQuantityMetadata(Quantity)
        });
        if (!Mathf.Approximately(luckCoefficient, 0f))
        {
            entry.ConfigureRuntimeRules(
                null,
                new ContentWeightRule[]
                {
                    new PlayerPropertyScaleWeightRule(
                        PropType.Luck,
                        luckCoefficient / DropSourceRuleData.LUCK_WEIGHT_DIVISOR,
                        0f,
                        0f)
                });
        }

        return entry;
    }

    private UnityEngine.Object ResolveContent(ContentPoolSO fallbackPool)
    {
        if (product != null)
        {
            return product;
        }

        return productPool != null ? productPool : fallbackPool;
    }

    private static string ResolveEntryId(UnityEngine.Object content, int index)
    {
        string contentName = content != null ? content.name : "None";
        return $"DropProduct_{index}_{contentName}";
    }
}
