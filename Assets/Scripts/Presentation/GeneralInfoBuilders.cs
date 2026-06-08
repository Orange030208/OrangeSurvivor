using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AccessoryInfoBuilder : IInfoDocumentBuilder<AccessoryDataSO>
{
    public InfoDocument Build(AccessoryDataSO source)
    {
        if (source == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument("缺失饰品数据");
        }

        List<InfoItem> items = new()
        {
            InfoDocumentUtility.CreateTitle(source.ItemName),
            InfoDocumentUtility.CreateLineBreak()
        };

        string imageKey = string.IsNullOrWhiteSpace(source.AccessoryId) ? source.name : source.AccessoryId;
        if (!string.IsNullOrWhiteSpace(imageKey))
        {
            items.Add(InfoDocumentUtility.CreateImage(
                imageKey,
                new AccessoryImage(source.ItemIcon)));
        }

        items.Add(InfoDocumentUtility.CreateTagText(ItemDescriptionUtility.FormatRarity(source.Tier)));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        items.Add(InfoDocumentUtility.CreateSectionHeader("基础"));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        InfoDocumentUtility.AppendTextLine(items, $"品质: {ItemDescriptionUtility.FormatRarity(source.Tier)}", InfoTone.Emphasis);

        if (source.HasOwnedLimit)
        {
            InfoDocumentUtility.AppendTextLine(items, $"持有上限: {source.MaxOwnedCount}");
        }

        if (source.RecyclePrice > 0)
        {
            InfoDocumentUtility.AppendTextLine(items, $"回收价格: {source.RecyclePrice}");
        }

        InfoDocumentContentUtility.AddModifierItems(items, "属性", source.PropertyModifiers);
        InfoDocumentContentUtility.AddFeatureItems(items, "特殊效果", source.SpecialFeatures);
        InfoDocumentContentUtility.AddDescriptionItems(items, "说明", source.ManualDescription);

        return new InfoDocument(
            string.IsNullOrWhiteSpace(source.AccessoryId) ? source.name : source.AccessoryId,
            items);
    }
}

public sealed class RewardCardInfoBuilder : IInfoDocumentBuilder<RewardCardSO>
{
    public InfoDocument Build(RewardCardSO source)
    {
        if (source == null)
        {
            return InfoDocumentContentUtility.BuildMissingDocument("缺失升级卡数据");
        }

        List<InfoItem> items = new()
        {
            InfoDocumentUtility.CreateTitle(source.Title),
            InfoDocumentUtility.CreateLineBreak()
        };

        string imageKey = string.IsNullOrWhiteSpace(source.Id) ? source.name : source.Id;
        if (!string.IsNullOrWhiteSpace(imageKey))
        {
            items.Add(InfoDocumentUtility.CreateImage(
                imageKey,
                new RewardCardImage(source.Icon)));
        }

        List<string> tags = InfoDocumentContentUtility.BuildUpgradeTagLabels(source.TagList);
        if (tags.Count > 0)
        {
            items.Add(InfoDocumentUtility.CreateTagText(string.Join(" / ", tags), InfoTone.Disabled));
            items.Add(InfoDocumentUtility.CreateLineBreak());
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader("基础"));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        InfoDocumentUtility.AppendTextLine(items, $"品质: {ItemDescriptionUtility.FormatRarity(source.Tier)}", InfoTone.Emphasis);
        InfoDocumentContentUtility.AddFeatureItems(items, "特殊效果", source.GrantedAbilities);
        if (source.GrantedAbilities == null || source.GrantedAbilities.Count == 0)
        {
            InfoDocumentContentUtility.AddDescriptionItems(items, "说明", source.ManualDescription);
        }

        return new InfoDocument(
            string.IsNullOrWhiteSpace(source.Id) ? source.name : source.Id,
            items);
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
            return InfoDocumentContentUtility.BuildMissingDocument("缺失 Buff 数据");
        }

        if (buffData == null)
        {
            InfoDocument fallbackDocument = fallback.BuildInfoDocument();
            return fallbackDocument ?? InfoDocumentContentUtility.BuildMissingDocument("缺失 Buff 数据");
        }

        List<InfoItem> items = new()
        {
            InfoDocumentUtility.CreateTitle(buffData.DisplayName),
            InfoDocumentUtility.CreateLineBreak()
        };

        string imageKey = buffData.BuffId;
        if (!string.IsNullOrWhiteSpace(imageKey))
        {
            items.Add(InfoDocumentUtility.CreateImage(
                imageKey,
                new BuffImage(buffData.Icon)));
        }

        items.Add(InfoDocumentUtility.CreateTagText(FormatPolarity(buffData.Polarity), ResolvePolarityTone(buffData.Polarity)));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        AddStateItems(items, source);
        AddRuleItems(items, buffData);
        InfoDocumentContentUtility.AddFeatureItems(items, "特殊效果", buffData.SpecialFeatures);
        if (buffData.SpecialFeatures == null || buffData.SpecialFeatures.Count == 0)
        {
            InfoDocumentContentUtility.AddDescriptionItems(items, "说明", buffData.ManualDescription);
        }

        return new InfoDocument(
            buffData.BuffId,
            items);
    }

    private static void AddStateItems(List<InfoItem> items, BuffInfoSource source)
    {
        List<InfoItem> stateItems = new();
        if (source.StackCount > 0)
        {
            string stackText = source.MaxStackCount > 0
                ? $"{source.StackCount} / {source.MaxStackCount}"
                : source.StackCount.ToString();
            InfoDocumentUtility.AppendTextLine(stateItems, $"层数: {stackText}", InfoTone.Emphasis);
        }

        if (source.HasDuration)
        {
            InfoDocumentUtility.AppendTextLine(stateItems, $"剩余时间: {source.RemainingDurationSeconds:0.0}s", InfoTone.Warning);
        }

        if (stateItems.Count == 0)
        {
            return;
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader("状态"));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        items.AddRange(stateItems);
    }

    private static void AddRuleItems(List<InfoItem> items, BuffDataSO buffData)
    {
        if (buffData == null)
        {
            return;
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader("规则"));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        InfoDocumentUtility.AppendTextLine(items, $"类型: {FormatPolarity(buffData.Polarity)}", ResolvePolarityTone(buffData.Polarity));
        InfoDocumentUtility.AppendTextLine(items, $"持续: {FormatDuration(buffData)}");

        if (buffData.MaxStackCount > 1)
        {
            InfoDocumentUtility.AppendTextLine(items, $"最大层数: {buffData.MaxStackCount}");
        }
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

internal static class InfoDocumentContentUtility
{
    public static InfoDocument BuildMissingDocument(string title)
    {
        return new InfoDocument(
            string.Empty,
            new[]
            {
                InfoDocumentUtility.CreateTitle(title),
                InfoDocumentUtility.CreateLineBreak(),
                InfoDocumentUtility.CreateSectionHeader("说明"),
                InfoDocumentUtility.CreateLineBreak(),
                InfoDocumentUtility.CreateText("无法生成详情：数据为空。", InfoTone.Warning),
                InfoDocumentUtility.CreateLineBreak()
            });
    }

    public static void AddModifierItems(
        List<InfoItem> items,
        string sectionTitle,
        IReadOnlyList<PropModifierData> modifiers)
    {
        if (items == null || modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader(sectionTitle));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        for (int i = 0; i < modifiers.Count; i++)
        {
            PropModifierData modifier = modifiers[i];
            InfoDocumentUtility.AppendPropertyLine(
                items,
                modifier.propType.ToString(),
                modifier.GetDisplayValueText(),
                ResolveModifierTone(modifier));
        }
    }

    public static void AddFeatureItems(
        List<InfoItem> items,
        string sectionTitle,
        IReadOnlyList<FeatureBase> features)
    {
        if (items == null || features == null || features.Count == 0)
        {
            return;
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader(sectionTitle));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        for (int i = 0; i < features.Count; i++)
        {
            FeatureBase feature = features[i];
            if (feature == null || string.IsNullOrWhiteSpace(feature.Description))
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(feature.Title) ? "特殊效果" : feature.Title;
            InfoDocumentUtility.AppendTextLine(items, $"{label}: {feature.Description}", InfoTone.Emphasis);
        }
    }

    public static void AddDescriptionItems(List<InfoItem> items, string sectionTitle, string description)
    {
        string normalizedDescription = ItemDescriptionUtility.NormalizeManualDescription(description);
        if (items == null || string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return;
        }

        items.Add(InfoDocumentUtility.CreateSectionHeader(sectionTitle));
        items.Add(InfoDocumentUtility.CreateLineBreak());
        InfoDocumentUtility.AppendTextLine(items, normalizedDescription);
    }

    public static List<string> BuildUpgradeTagLabels(IReadOnlyList<CardTag> tags)
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
