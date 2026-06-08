using System;
using System.Collections.Generic;
using System.Text;

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
        IEnumerable<InfoItem> extraItems,
        string fallbackText)
    {
        List<InfoItem> items = new();
        AddFlavorItem(items, description);
        AddPropertyItems(items, propertyModifiers);
        AddFeatureItems(items, specialFeatures);
        AddExtraItems(items, extraItems);

        if (items.Count == 0)
        {
            return string.IsNullOrWhiteSpace(fallbackText) ? string.Empty : fallbackText;
        }

        return InfoDocumentTextFormatter.ToPlainText(
            new InfoDocument(string.Empty, items),
            includeHeader: false);
    }

    public static string FormatRarity(ContentTier tier)
    {
        return tier switch
        {
            ContentTier.Common => "普通",
            ContentTier.Rare => "稀有",
            ContentTier.Epic => "史诗",
            ContentTier.Legendary => "传说",
            _ => tier.ToString()
        };
    }

    public static string FormatUpgradeCardTag(CardTag tag)
    {
        return tag switch
        {
            CardTag.Attack => "攻击",
            CardTag.Defense => "防御",
            CardTag.Critical => "暴击",
            CardTag.AttackSpeed => "攻速",
            CardTag.MoveSpeed => "移动",
            CardTag.Pickup => "拾取",
            CardTag.Economy => "经济",
            CardTag.Weapon => "武器",
            CardTag.Melee => "近战",
            CardTag.Ranged => "远程",
            CardTag.Projectile => "投射物",
            CardTag.Recovery => "回复",
            CardTag.LowHealth => "低血",
            CardTag.AreaDamage => "范围",
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

    public static string JoinUpgradeCardTags(IReadOnlyList<CardTag> tags, int maxCount)
    {
        if (tags == null || tags.Count == 0 || maxCount <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
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

    public static string JoinUpgradeCardTags(CardTag tags, int maxCount)
    {
        return JoinUpgradeCardTags(ToUpgradeCardTagArray(tags), maxCount);
    }

    public static string JoinWeaponTags(IReadOnlyList<WeaponTag> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
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

    private static void AddFlavorItem(List<InfoItem> items, string description)
    {
        string normalizedDescription = NormalizeManualDescription(description);
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            return;
        }

        InfoDocumentUtility.AppendTextLine(items, normalizedDescription);
    }

    private static void AddPropertyItems(List<InfoItem> items, IReadOnlyList<PropModifierData> propertyModifiers)
    {
        if (propertyModifiers == null)
        {
            return;
        }

        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            PropModifierData modifier = propertyModifiers[i];
            InfoDocumentUtility.AppendPropertyLine(
                items,
                modifier.propType.ToString(),
                modifier.GetDisplayValueText(),
                modifier.value > 0f ? InfoTone.Positive : modifier.value < 0f ? InfoTone.Negative : InfoTone.Neutral);
        }
    }

    private static void AddFeatureItems(List<InfoItem> items, IReadOnlyList<FeatureBase> specialFeatures)
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
            InfoDocumentUtility.AppendTextLine(items, $"{label}: {feature.Description}", InfoTone.Emphasis);
        }
    }

    private static void AddExtraItems(List<InfoItem> items, IEnumerable<InfoItem> extraItems)
    {
        if (extraItems == null)
        {
            return;
        }

        foreach (InfoItem item in extraItems)
        {
            if (string.IsNullOrWhiteSpace(item.Content) &&
                item.Type != InfoItemType.LineBreak &&
                item.Type != InfoItemType.Spacer)
            {
                continue;
            }

            items.Add(item);
        }
    }

    private static CardTag[] ToUpgradeCardTagArray(CardTag mask)
    {
        if (mask == CardTag.None)
        {
            return Array.Empty<CardTag>();
        }

        List<CardTag> result = new();
        foreach (CardTag tag in Enum.GetValues(typeof(CardTag)))
        {
            if (tag == CardTag.None || (mask & tag) == 0)
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
