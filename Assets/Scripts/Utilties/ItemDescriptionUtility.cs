using System;
using System.Collections.Generic;
using System.Text;

internal enum ItemDescriptionLineKind
{
    Flavor,
    Property,
    Feature,
    Meta
}

internal readonly struct ItemDescriptionLine
{
    public ItemDescriptionLine(string label, string value, ItemDescriptionLineKind kind)
    {
        Label = label;
        Value = value;
        Kind = kind;
    }

    public string Label { get; }
    public string Value { get; }
    public ItemDescriptionLineKind Kind { get; }
}

internal static class ItemDescriptionUtility
{
    public static string NormalizeManualDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        string trimmedDescription = description.Trim();
        return IsPlaceholderDescription(trimmedDescription) ? string.Empty : trimmedDescription;
    }

    public static string BuildDetailedDescription(
        string description,
        IReadOnlyList<PropModifierData> propertyModifiers,
        IReadOnlyList<FeatureEffectBase> specialFeatures,
        string fallbackText)
    {
        return BuildDetailedDescription(
            description,
            propertyModifiers,
            specialFeatures,
            null,
            fallbackText);
    }

    public static string BuildDetailedDescription(
        string description,
        IReadOnlyList<PropModifierData> propertyModifiers,
        IReadOnlyList<FeatureEffectBase> specialFeatures,
        IEnumerable<ItemDescriptionLine> extraLines,
        string fallbackText)
    {
        List<ItemDescriptionLine> lines = new List<ItemDescriptionLine>();
        AddFlavorLine(lines, description);
        AddPropertyLines(lines, propertyModifiers);
        AddFeatureLines(lines, specialFeatures);
        AddExtraLines(lines, extraLines);

        if (lines.Count == 0)
        {
            return string.IsNullOrWhiteSpace(fallbackText) ? string.Empty : fallbackText;
        }

        return BuildLinesText(lines);
    }

    public static List<DescriptorInfo> BuildDescriptorInfos(
        string description,
        IReadOnlyList<PropModifierData> propertyModifiers,
        IReadOnlyList<FeatureEffectBase> specialFeatures)
    {
        return BuildDescriptorInfos(
            description,
            propertyModifiers,
            specialFeatures,
            null);
    }

    public static List<DescriptorInfo> BuildDescriptorInfos(
        string description,
        IReadOnlyList<PropModifierData> propertyModifiers,
        IReadOnlyList<FeatureEffectBase> specialFeatures,
        IEnumerable<DescriptorInfo> extraInfos)
    {
        List<DescriptorInfo> infos = new List<DescriptorInfo>();
        AddDescriptionInfo(infos, description);
        AddPropertyInfos(infos, propertyModifiers);
        AddFeatureInfos(infos, specialFeatures);
        AddDescriptorInfos(infos, extraInfos);
        return infos;
    }

    public static string FormatDescriptorInfo(DescriptorInfo descriptorInfo)
    {
        string label = descriptorInfo.label;
        string value = descriptorInfo.value;

        if (string.IsNullOrWhiteSpace(label))
        {
            return value ?? string.Empty;
        }

        if (IsDescriptionLabel(label))
        {
            return value ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return label;
        }

        return $"{label}: {value}";
    }

    public static string FormatRarity(AccessoryRarity rarity)
    {
        return rarity switch
        {
            AccessoryRarity.Common => "普通",
            AccessoryRarity.Rare => "稀有",
            AccessoryRarity.Epic => "史诗",
            AccessoryRarity.Legendary => "传说",
            _ => rarity.ToString()
        };
    }

    public static string FormatRarity(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => "普通",
            UpgradeCardRarity.Rare => "稀有",
            UpgradeCardRarity.Epic => "史诗",
            UpgradeCardRarity.Legendary => "传说",
            _ => rarity.ToString()
        };
    }

    public static string FormatUpgradeCardTag(UpgradeCardTag tag)
    {
        return tag switch
        {
            UpgradeCardTag.Attack => "攻击",
            UpgradeCardTag.Defense => "防御",
            UpgradeCardTag.Critical => "暴击",
            UpgradeCardTag.AttackSpeed => "攻速",
            UpgradeCardTag.MoveSpeed => "移动",
            UpgradeCardTag.Pickup => "拾取",
            UpgradeCardTag.Economy => "经济",
            UpgradeCardTag.Weapon => "武器",
            UpgradeCardTag.Melee => "近战",
            UpgradeCardTag.Ranged => "远程",
            UpgradeCardTag.Projectile => "投射物",
            UpgradeCardTag.Recovery => "回复",
            UpgradeCardTag.LowHealth => "低血",
            UpgradeCardTag.AreaDamage => "范围",
            _ => tag.ToString()
        };
    }

    public static string FormatWeaponTag(WeaponTag tag)
    {
        return tag switch
        {
            WeaponTag.Melee => "近战",
            WeaponTag.Ranged => "远程",
            WeaponTag.Projectile => "投射物",
            WeaponTag.AreaDamage => "范围伤害",
            WeaponTag.Critical => "暴击",
            WeaponTag.Fast => "快速",
            WeaponTag.Heavy => "重型",
            _ => tag.ToString()
        };
    }

    public static string JoinUpgradeCardTags(IReadOnlyList<UpgradeCardTag> tags, int maxCount)
    {
        if (tags == null || tags.Count == 0 || maxCount <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        int count = tags.Count < maxCount ? tags.Count : maxCount;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append("/");
            }

            builder.Append(FormatUpgradeCardTag(tags[i]));
        }

        return builder.ToString();
    }

    public static string JoinWeaponTags(IReadOnlyList<WeaponTag> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < tags.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(FormatWeaponTag(tags[i]));
        }

        return builder.ToString();
    }

    public static string FormatWeaponStatValue(PropType propType, float value)
    {
        return propType switch
        {
            PropType.AttackSpeed => $"{value:0.##}/秒",
            PropType.CriticalChance => $"{value:0.##}%",
            PropType.CriticalPercent => $"{value:0.##}%",
            PropType.AttackRange => $"{PropValueUtility.DistancePointsToWorldUnits(value):0.##}格",
            _ => value.ToString("0.##")
        };
    }

    private static void AddFlavorLine(List<ItemDescriptionLine> lines, string description)
    {
        string normalizedDescription = NormalizeManualDescription(description);
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return;
        }

        lines.Add(new ItemDescriptionLine(string.Empty, normalizedDescription, ItemDescriptionLineKind.Flavor));
    }

    private static void AddPropertyLines(List<ItemDescriptionLine> lines, IReadOnlyList<PropModifierData> propertyModifiers)
    {
        if (propertyModifiers == null)
        {
            return;
        }

        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            PropModifierData modifier = propertyModifiers[i];
            lines.Add(new ItemDescriptionLine(
                modifier.GetDisplayName(),
                modifier.GetDisplayValueText(),
                ItemDescriptionLineKind.Property));
        }
    }

    private static void AddFeatureLines(List<ItemDescriptionLine> lines, IReadOnlyList<FeatureEffectBase> specialFeatures)
    {
        if (specialFeatures == null)
        {
            return;
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            FeatureEffectBase feature = specialFeatures[i];
            if (feature == null || string.IsNullOrWhiteSpace(feature.Description))
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(feature.Title) ? "特殊效果" : feature.Title;
            lines.Add(new ItemDescriptionLine(label, feature.Description, ItemDescriptionLineKind.Feature));
        }
    }

    private static void AddExtraLines(List<ItemDescriptionLine> lines, IEnumerable<ItemDescriptionLine> extraLines)
    {
        if (extraLines == null)
        {
            return;
        }

        foreach (ItemDescriptionLine line in extraLines)
        {
            if (string.IsNullOrWhiteSpace(line.Label) && string.IsNullOrWhiteSpace(line.Value))
            {
                continue;
            }

            lines.Add(line);
        }
    }

    private static string BuildLinesText(IReadOnlyList<ItemDescriptionLine> lines)
    {
        StringBuilder builder = new StringBuilder();
        ItemDescriptionLineKind? previousKind = null;
        for (int i = 0; i < lines.Count; i++)
        {
            ItemDescriptionLine line = lines[i];
            if (previousKind.HasValue && previousKind.Value != line.Kind)
            {
                builder.AppendLine();
            }

            builder.Append(FormatLine(line));

            if (i < lines.Count - 1)
            {
                builder.AppendLine();
            }

            previousKind = line.Kind;
        }

        return builder.ToString();
    }

    private static string FormatLine(ItemDescriptionLine line)
    {
        if (string.IsNullOrWhiteSpace(line.Label))
        {
            return line.Value ?? string.Empty;
        }

        if (line.Kind == ItemDescriptionLineKind.Flavor || IsDescriptionLabel(line.Label))
        {
            return line.Value ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(line.Value))
        {
            return line.Label;
        }

        return $"{line.Label}: {line.Value}";
    }

    private static void AddDescriptionInfo(List<DescriptorInfo> infos, string description)
    {
        string normalizedDescription = NormalizeManualDescription(description);
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return;
        }

        infos.Add(new DescriptorInfo(string.Empty, normalizedDescription));
    }

    private static void AddPropertyInfos(List<DescriptorInfo> infos, IReadOnlyList<PropModifierData> propertyModifiers)
    {
        if (propertyModifiers == null)
        {
            return;
        }

        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            PropModifierData modifier = propertyModifiers[i];
            infos.Add(new DescriptorInfo(modifier.GetDisplayName(), modifier.GetDisplayValueText()));
        }
    }

    private static void AddFeatureInfos(List<DescriptorInfo> infos, IReadOnlyList<FeatureEffectBase> specialFeatures)
    {
        if (specialFeatures == null)
        {
            return;
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            FeatureEffectBase feature = specialFeatures[i];
            if (feature == null || string.IsNullOrWhiteSpace(feature.Description))
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(feature.Title) ? "特殊效果" : feature.Title;
            infos.Add(new DescriptorInfo(label, feature.Description));
        }
    }

    private static void AddDescriptorInfos(List<DescriptorInfo> infos, IEnumerable<DescriptorInfo> extraInfos)
    {
        if (extraInfos == null)
        {
            return;
        }

        foreach (DescriptorInfo info in extraInfos)
        {
            if (string.IsNullOrWhiteSpace(info.label) && string.IsNullOrWhiteSpace(info.value))
            {
                continue;
            }

            infos.Add(info);
        }
    }

    private static bool IsDescriptionLabel(string label)
    {
        return string.Equals(label?.Trim(), "说明", StringComparison.Ordinal);
    }

    private static bool IsPlaceholderDescription(string description)
    {
        string compactDescription = description
            .Replace(" ", string.Empty)
            .Replace("　", string.Empty)
            .Replace("。", string.Empty)
            .Replace(".", string.Empty);

        return string.Equals(compactDescription, "纯属性饰品", StringComparison.Ordinal);
    }
}
