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
        IReadOnlyList<FeatureBase> specialFeatures,
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
        IReadOnlyList<FeatureBase> specialFeatures,
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
            WeaponTag.Heavy => "重型",
            WeaponTag.Fast => "快速",
            WeaponTag.Growth => "成长",
            WeaponTag.Precision => "精准",
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

    public static string JoinUpgradeCardTags(UpgradeCardTag tags, int maxCount)
    {
        return JoinUpgradeCardTags(ToUpgradeCardTagArray(tags), maxCount);
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
            PropType.AttackSpeed => $"{value:0}",
            PropType.CriticalChance => $"{value:0.##}%",
            PropType.CriticalPercent => $"{value:0.##}%",
            PropType.AttackRange => $"{value:0.##}",
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

    private static void AddFeatureLines(List<ItemDescriptionLine> lines, IReadOnlyList<FeatureBase> specialFeatures)
    {
        if (specialFeatures == null)
        {
            return;
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            FeatureBase feature = specialFeatures[i];
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

    private static bool IsDescriptionLabel(string label)
    {
        return string.Equals(label?.Trim(), "说明", StringComparison.Ordinal);
    }

    private static UpgradeCardTag[] ToUpgradeCardTagArray(UpgradeCardTag mask)
    {
        if (mask == UpgradeCardTag.None)
        {
            return Array.Empty<UpgradeCardTag>();
        }

        List<UpgradeCardTag> result = new();
        foreach (UpgradeCardTag tag in Enum.GetValues(typeof(UpgradeCardTag)))
        {
            if (tag == UpgradeCardTag.None || (mask & tag) == 0)
            {
                continue;
            }

            result.Add(tag);
        }

        return result.ToArray();
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
