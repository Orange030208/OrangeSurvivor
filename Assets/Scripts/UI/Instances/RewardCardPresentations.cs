using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RewardCardPresentationBase : IRewardCardPresentation
{
    protected RewardCardPresentationBase(
        string optionId,
        RewardOptionKind kind,
        RewardCardStyle style,
        string title,
        Sprite icon,
        string description,
        ContentTier tier,
        bool interactable)
    {
        OptionId = optionId ?? string.Empty;
        Kind = kind;
        Style = style;
        Title = title ?? string.Empty;
        Icon = icon;
        Description = description ?? string.Empty;
        Tier = tier;
        Interactable = interactable;
    }

    public string OptionId { get; }
    public RewardOptionKind Kind { get; }
    public RewardCardStyle Style { get; }
    public string Title { get; }
    public Sprite Icon { get; }
    public string Description { get; }
    public ContentTier Tier { get; }
    public bool Interactable { get; }

    protected static string BuildQualityDescription(string description, ContentTier tier)
    {
        return $"{GetQualityText(tier)}\n{description}";
    }

    protected static string GetQualityText(ContentTier tier)
    {
        return tier switch
        {
            ContentTier.Rare => "稀有",
            ContentTier.Epic => "史诗",
            ContentTier.Legendary => "传说",
            _ => "普通"
        };
    }
}

public sealed class UpgradeRewardCardPresentation : RewardCardPresentationBase
{
    private readonly string[] tags;

    public UpgradeRewardCardPresentation(
        string optionId,
        string title,
        string description,
        ContentTier tier,
        string[] tags,
        bool interactable)
        : base(
            optionId,
            RewardOptionKind.UpgradeCard,
            RewardCardStyle.UpgradeCard,
            title,
            null,
            description,
            tier,
            interactable)
    {
        this.tags = tags ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Tags => tags;

}

public sealed class EquipmentRewardCardPresentation : RewardCardPresentationBase
{
    public EquipmentRewardCardPresentation(
        RewardOptionKind kind,
        string optionId,
        string title,
        Sprite icon,
        string description,
        ContentTier tier,
        bool interactable)
        : base(
            optionId,
            kind,
            RewardCardStyle.EquipmentReward,
            title,
            icon,
            description,
            tier,
            interactable)
    {
    }
}
