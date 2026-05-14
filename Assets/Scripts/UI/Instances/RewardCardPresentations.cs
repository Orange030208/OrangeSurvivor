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
        CardQuality quality,
        bool interactable)
    {
        OptionId = optionId ?? string.Empty;
        Kind = kind;
        Style = style;
        Title = title ?? string.Empty;
        Icon = icon;
        Description = description ?? string.Empty;
        Quality = quality;
        Interactable = interactable;
    }

    public string OptionId { get; }
    public RewardOptionKind Kind { get; }
    public RewardCardStyle Style { get; }
    public string Title { get; }
    public Sprite Icon { get; }
    public string Description { get; }
    public CardQuality Quality { get; }
    public bool Interactable { get; }
    public abstract IEnumerable<DescriptorInfo> GetExtraInfos();

    protected static string BuildQualityDescription(string description, CardQuality quality)
    {
        return $"{GetQualityText(quality)}\n{description}";
    }

    protected static string GetQualityText(CardQuality quality)
    {
        return quality switch
        {
            CardQuality.Rare => "稀有",
            CardQuality.Epic => "史诗",
            CardQuality.Legendary => "传说",
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
        CardQuality quality,
        string[] tags,
        bool interactable)
        : base(
            optionId,
            RewardOptionKind.UpgradeCard,
            RewardCardStyle.UpgradeCard,
            title,
            null,
            description,
            quality,
            interactable)
    {
        this.tags = tags ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Tags => tags;

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        string description = BuildDescription(Description, Quality, tags);
        if (!string.IsNullOrWhiteSpace(description))
        {
            yield return new DescriptorInfo(string.Empty, description);
        }
    }

    private static string BuildDescription(string description, CardQuality quality, string[] tags)
    {
        string tagText = BuildTagText(tags);
        return $"{GetQualityText(quality)}{tagText}\n{description}";
    }

    private static string BuildTagText(string[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return string.Empty;
        }

        int count = Mathf.Min(2, tags.Length);
        string result = " · ";
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result += "/";
            }

            result += tags[i];
        }

        return result;
    }
}

public sealed class EquipmentRewardCardPresentation : RewardCardPresentationBase
{
    public EquipmentRewardCardPresentation(
        RewardOptionKind kind,
        string optionId,
        IDescribable describable,
        CardQuality quality,
        bool interactable)
        : base(
            optionId,
            kind,
            RewardCardStyle.EquipmentReward,
            describable != null ? describable.Title : string.Empty,
            describable != null ? describable.Icon : null,
            describable != null ? describable.Description : string.Empty,
            quality,
            interactable)
    {
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        string description = BuildQualityDescription(Description, Quality);
        if (!string.IsNullOrWhiteSpace(description))
        {
            yield return new DescriptorInfo(string.Empty, description);
        }
    }
}
