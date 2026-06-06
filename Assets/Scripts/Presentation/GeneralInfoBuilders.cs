using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AccessoryInfoBuilder : IInfoDocumentBuilder<AccessoryDataSO>
{
    private const string MetaSectionTitle = "基础";
    private const string StatsSectionTitle = "属性";
    private const string EffectsSectionTitle = "特殊效果";
    private const string DescriptionSectionTitle = "说明";

    public InfoDocument Build(AccessoryDataSO source)
    {
        if (source == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument(InfoDocumentKind.Accessory, "缺失饰品数据");
        }

        List<InfoSection> sections = new();
        List<InfoLine> metaLines = new()
        {
            InfoDocumentUtility.CreateSingleValueLine("品质", ItemDescriptionUtility.FormatRarity(source.Tier), InfoTone.Emphasis)
        };

        if (source.HasOwnedLimit)
        {
            metaLines.Add(InfoDocumentUtility.CreateSingleValueLine("持有上限", source.MaxOwnedCount.ToString()));
        }

        if (source.RecyclePrice > 0)
        {
            metaLines.Add(InfoDocumentUtility.CreateSingleValueLine("回收价格", source.RecyclePrice.ToString()));
        }

        sections.Add(new InfoSection(MetaSectionTitle, metaLines));
        InfoDocumentContentUtility.AddModifierSection(sections, StatsSectionTitle, source.PropertyModifiers);
        InfoDocumentContentUtility.AddFeatureSection(sections, EffectsSectionTitle, source.SpecialFeatures);
        InfoDocumentContentUtility.AddDescriptionSection(sections, DescriptionSectionTitle, source.ManualDescription);

        return new InfoDocument(
            string.IsNullOrWhiteSpace(source.AccessoryId) ? source.name : source.AccessoryId,
            source.ItemName,
            source.ItemIcon,
            InfoDocumentKind.Accessory,
            new[] { ItemDescriptionUtility.FormatRarity(source.Tier) },
            sections);
    }
}

public sealed class UpgradeCardInfoBuilder : IInfoDocumentBuilder<UpgradeCardSO>
{
    private const string MetaSectionTitle = "基础";
    private const string EffectsSectionTitle = "特殊效果";
    private const string DescriptionSectionTitle = "说明";

    public InfoDocument Build(UpgradeCardSO source)
    {
        if (source == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument(InfoDocumentKind.UpgradeCard, "缺失升级卡数据");
        }

        List<InfoSection> sections = new();
        List<string> tags = InfoDocumentContentUtility.BuildUpgradeTagLabels(source.TagList);
        sections.Add(new InfoSection(
            MetaSectionTitle,
            new[]
            {
                InfoDocumentUtility.CreateSingleValueLine("品质", ItemDescriptionUtility.FormatRarity(source.Rarity), InfoTone.Emphasis)
            }));

        InfoDocumentContentUtility.AddFeatureSection(sections, EffectsSectionTitle, source.SpecialFeatures);
        if (source.SpecialFeatures == null || source.SpecialFeatures.Count == 0)
        {
            InfoDocumentContentUtility.AddDescriptionSection(sections, DescriptionSectionTitle, source.Description);
        }

        return new InfoDocument(
            string.IsNullOrWhiteSpace(source.CardId) ? source.name : source.CardId,
            source.Title,
            null,
            InfoDocumentKind.UpgradeCard,
            tags,
            sections);
    }
}

public readonly struct BuffInfoSource
{
    public BuffInfoSource(
        BuffDataSO buffData,
        IInfoDocumentSource fallbackInfoSource,
        int stackCount,
        int maxStackCount,
        bool hasDuration,
        float remainingDurationSeconds,
        float totalDurationSeconds)
    {
        BuffData = buffData;
        FallbackInfoSource = fallbackInfoSource;
        StackCount = Mathf.Max(0, stackCount);
        MaxStackCount = Mathf.Max(0, maxStackCount);
        HasDuration = hasDuration;
        RemainingDurationSeconds = Mathf.Max(0f, remainingDurationSeconds);
        TotalDurationSeconds = Mathf.Max(0f, totalDurationSeconds);
    }

    public BuffDataSO BuffData { get; }
    public IInfoDocumentSource FallbackInfoSource { get; }
    public int StackCount { get; }
    public int MaxStackCount { get; }
    public bool HasDuration { get; }
    public float RemainingDurationSeconds { get; }
    public float TotalDurationSeconds { get; }

    public static BuffInfoSource FromData(BuffDataSO buffData)
    {
        return new BuffInfoSource(
            buffData,
            buffData,
            stackCount: 0,
            maxStackCount: buffData != null ? buffData.MaxStackCount : 0,
            hasDuration: buffData != null && buffData.DurationPolicy == BuffDurationPolicy.Timed,
            remainingDurationSeconds: buffData != null ? buffData.DurationSeconds : 0f,
            totalDurationSeconds: buffData != null ? buffData.DurationSeconds : 0f);
    }

    public static BuffInfoSource FromViewData(ActiveBuffViewData viewData)
    {
        return new BuffInfoSource(
            viewData.InfoSource as BuffDataSO,
            viewData.InfoSource,
            viewData.StackCount,
            viewData.MaxStackCount,
            viewData.HasDuration,
            viewData.RemainingDurationSeconds,
            viewData.TotalDurationSeconds);
    }
}

public sealed class BuffInfoBuilder :
    IInfoDocumentBuilder<BuffDataSO>,
    IInfoDocumentBuilder<BuffInfoSource>,
    IInfoDocumentBuilder<ActiveBuffViewData>
{
    private const string StateSectionTitle = "状态";
    private const string RulesSectionTitle = "规则";
    private const string EffectsSectionTitle = "特殊效果";
    private const string DescriptionSectionTitle = "说明";

    public InfoDocument Build(BuffDataSO source)
    {
        return Build(BuffInfoSource.FromData(source));
    }

    public InfoDocument Build(ActiveBuffViewData source)
    {
        return Build(BuffInfoSource.FromViewData(source));
    }

    public InfoDocument Build(BuffInfoSource source)
    {
        BuffDataSO buffData = source.BuffData;
        IInfoDocumentSource fallback = source.FallbackInfoSource;
        if (buffData == null && fallback == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument(InfoDocumentKind.Buff, "缺失 Buff 数据");
        }

        List<InfoSection> sections = new();
        AddStateSection(sections, source);
        AddRulesSection(sections, buffData);
        if (buffData != null)
        {
            InfoDocumentContentUtility.AddFeatureSection(sections, EffectsSectionTitle, buffData.SpecialFeatures);
            if (buffData.SpecialFeatures == null || buffData.SpecialFeatures.Count == 0)
            {
                InfoDocumentContentUtility.AddDescriptionSection(sections, DescriptionSectionTitle, buffData.Description);
            }
        }
        else
        {
            InfoDocument fallbackDocument = fallback.BuildInfoDocument();
            return fallbackDocument ?? InfoDocumentContentUtility.BuildMissingDocument(InfoDocumentKind.Buff, "缺失 Buff 数据");
        }

        return new InfoDocument(
            buffData.BuffId,
            buffData.DisplayName,
            buffData.Icon,
            InfoDocumentKind.Buff,
            new[] { FormatPolarity(buffData.Polarity) },
            sections);
    }

    private static void AddStateSection(List<InfoSection> sections, BuffInfoSource source)
    {
        List<InfoLine> lines = new();
        if (source.StackCount > 0)
        {
            string stackText = source.MaxStackCount > 0
                ? $"{source.StackCount} / {source.MaxStackCount}"
                : source.StackCount.ToString();
            lines.Add(InfoDocumentUtility.CreateSingleValueLine("层数", stackText, InfoTone.Emphasis));
        }

        if (source.HasDuration)
        {
            lines.Add(InfoDocumentUtility.CreateSingleValueLine(
                "剩余时间",
                $"{source.RemainingDurationSeconds:0.0}s",
                InfoTone.Warning));
        }

        if (lines.Count > 0)
        {
            sections.Add(new InfoSection(StateSectionTitle, lines));
        }
    }

    private static void AddRulesSection(List<InfoSection> sections, BuffDataSO buffData)
    {
        if (buffData == null)
        {
            return;
        }

        List<InfoLine> lines = new()
        {
            InfoDocumentUtility.CreateSingleValueLine("类型", FormatPolarity(buffData.Polarity), ResolvePolarityTone(buffData.Polarity)),
            InfoDocumentUtility.CreateSingleValueLine("持续", FormatDuration(buffData))
        };

        if (buffData.MaxStackCount > 1)
        {
            lines.Add(InfoDocumentUtility.CreateSingleValueLine("最大层数", buffData.MaxStackCount.ToString()));
        }

        sections.Add(new InfoSection(RulesSectionTitle, lines));
    }

    private static string FormatDuration(BuffDataSO buffData)
    {
        return buffData.DurationPolicy == BuffDurationPolicy.Timed
            ? $"{buffData.DurationSeconds:0.##}s"
            : "永久";
    }

    private static string FormatPolarity(BuffPolarity polarity)
    {
        return polarity switch
        {
            BuffPolarity.Positive => "增益",
            BuffPolarity.Negative => "减益",
            _ => "中性"
        };
    }

    private static InfoTone ResolvePolarityTone(BuffPolarity polarity)
    {
        return polarity switch
        {
            BuffPolarity.Positive => InfoTone.Positive,
            BuffPolarity.Negative => InfoTone.Negative,
            _ => InfoTone.Neutral
        };
    }
}

public sealed class RewardCardInfoBuilder : IInfoDocumentBuilder<IRewardCardPresentation>
{
    private const string MetaSectionTitle = "基础";
    private const string DescriptionSectionTitle = "说明";

    public InfoDocument Build(IRewardCardPresentation source)
    {
        if (source == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument(InfoDocumentKind.General, "缺失奖励数据");
        }

        List<InfoSection> sections = new()
        {
            new InfoSection(
                MetaSectionTitle,
                new[]
                {
                    InfoDocumentUtility.CreateSingleValueLine("品质", FormatQuality(source.Tier), InfoTone.Emphasis)
                })
        };

        InfoDocumentContentUtility.AddDescriptionSection(sections, DescriptionSectionTitle, source.Description);
        return new InfoDocument(
            source.OptionId,
            source.Title,
            source.Icon,
            ResolveDocumentKind(source.Kind),
            BuildTags(source),
            sections);
    }

    private static InfoDocumentKind ResolveDocumentKind(RewardOptionKind kind)
    {
        return kind switch
        {
            RewardOptionKind.Weapon => InfoDocumentKind.Weapon,
            RewardOptionKind.Accessory => InfoDocumentKind.Accessory,
            RewardOptionKind.UpgradeCard => InfoDocumentKind.UpgradeCard,
            _ => InfoDocumentKind.General
        };
    }

    private static IReadOnlyList<string> BuildTags(IRewardCardPresentation source)
    {
        if (source is UpgradeRewardCardPresentation upgradeReward)
        {
            return upgradeReward.Tags;
        }

        return new[] { FormatQuality(source.Tier) };
    }

    private static string FormatQuality(ContentTier tier)
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

internal static class InfoDocumentContentUtility
{
    public static InfoDocument BuildMissingDocument(InfoDocumentKind kind, string title)
    {
        return new InfoDocument(
            string.Empty,
            title,
            null,
            kind,
            Array.Empty<string>(),
            new[]
            {
                new InfoSection(
                    "说明",
                    new[] { InfoDocumentUtility.CreateSingleValueLine(string.Empty, "无法生成详情：数据为空。", InfoTone.Warning) })
            });
    }

    public static void AddModifierSection(
        List<InfoSection> sections,
        string title,
        IReadOnlyList<PropModifierData> modifiers)
    {
        if (sections == null || modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        List<InfoLine> lines = new();
        for (int i = 0; i < modifiers.Count; i++)
        {
            PropModifierData modifier = modifiers[i];
            lines.Add(InfoDocumentUtility.CreateSingleValueLine(
                modifier.GetDisplayName(),
                modifier.GetDisplayValueText(),
                ResolveModifierTone(modifier)));
        }

        if (lines.Count > 0)
        {
            sections.Add(new InfoSection(title, lines));
        }
    }

    public static void AddFeatureSection(
        List<InfoSection> sections,
        string title,
        IReadOnlyList<FeatureBase> features)
    {
        if (sections == null || features == null || features.Count == 0)
        {
            return;
        }

        List<InfoLine> lines = new();
        for (int i = 0; i < features.Count; i++)
        {
            FeatureBase feature = features[i];
            if (feature == null || string.IsNullOrWhiteSpace(feature.Description))
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(feature.Title) ? "特殊效果" : feature.Title;
            lines.Add(InfoDocumentUtility.CreateSingleValueLine(label, feature.Description, InfoTone.Emphasis));
        }

        if (lines.Count > 0)
        {
            sections.Add(new InfoSection(title, lines));
        }
    }

    public static void AddDescriptionSection(List<InfoSection> sections, string title, string description)
    {
        string normalizedDescription = ItemDescriptionUtility.NormalizeManualDescription(description);
        if (sections == null || string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return;
        }

        sections.Add(new InfoSection(
            title,
            new[] { InfoDocumentUtility.CreateSingleValueLine(string.Empty, normalizedDescription) }));
    }

    public static List<string> BuildUpgradeTagLabels(IReadOnlyList<UpgradeCardTag> tags)
    {
        List<string> labels = new();
        if (tags == null)
        {
            return labels;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            labels.Add(ItemDescriptionUtility.FormatUpgradeCardTag(tags[i]));
        }

        return labels;
    }

    private static InfoTone ResolveModifierTone(PropModifierData modifier)
    {
        if (Mathf.Approximately(modifier.value, 0f))
        {
            return InfoTone.Neutral;
        }

        return modifier.value > 0f ? InfoTone.Positive : InfoTone.Negative;
    }
}
