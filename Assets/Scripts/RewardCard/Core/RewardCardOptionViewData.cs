using UnityEngine;

public readonly struct RewardCardOptionViewData
{
    public readonly string Id;
    public readonly string Title;
    public readonly Sprite Icon;
    public readonly string Description;
    public readonly ContentTier Tier;
    public readonly CardTag[] Tags;
    public readonly int PickCount;
    public readonly int MaxPickCount;
    public readonly bool HasPickLimit;

    public RewardCardOptionViewData(
        string id,
        string title,
        Sprite icon,
        string description,
        ContentTier tier,
        CardTag[] tags,
        int pickCount,
        int maxPickCount,
        bool hasPickLimit)
    {
        Id = id;
        Title = title;
        Icon = icon;
        Description = description;
        Tier = tier;
        Tags = tags ?? System.Array.Empty<CardTag>();
        PickCount = Mathf.Max(0, pickCount);
        HasPickLimit = hasPickLimit;
        MaxPickCount = hasPickLimit ? Mathf.Max(1, maxPickCount) : 0;
    }
}
