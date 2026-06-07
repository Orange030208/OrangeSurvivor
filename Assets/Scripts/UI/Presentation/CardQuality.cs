using System;
using UnityEngine;

public interface IHasContentTier
{
    ContentTier Tier { get; }
}

public interface IContentTierConsumer
{
    bool Consume(ContentTier tier);
}

public static class ContentTierConsumerExtensions
{
    public static bool Consume(this IContentTierConsumer consumer, IHasContentTier source)
    {
        return consumer != null && source != null && consumer.Consume(source.Tier);
    }
}

public enum ContentTier
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

public static class ContentTierResolver
{
    public static ContentTier FromAccessoryTier(ContentTier tier)
    {
        return ClampAccessoryTier(tier);
    }

    public static ContentTier FromAccessoryTier(int tier)
    {
        return ClampAccessoryTier((ContentTier)tier);
    }

    public static ContentTier FromWeaponLevel(int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        return clampedLevel switch
        {
            2 => ContentTier.Rare,
            3 => ContentTier.Epic,
            4 => ContentTier.Legendary,
            _ => ContentTier.Common
        };
    }

    public static ContentTier FromItem(ItemDataSO itemData, int sourceValue)
    {
        if (itemData == null)
        {
            return ContentTier.Common;
        }

        return itemData.ItemType switch
        {
            ItemType.Accessory => FromAccessoryTier(sourceValue),
            ItemType.Weapon => FromWeaponLevel(sourceValue),
            _ => ContentTier.Common
        };
    }

    public static ContentTier FromQualityValue(int qualityValue)
    {
        int clampedValue = Mathf.Clamp(qualityValue, (int)ContentTier.Common, (int)ContentTier.Legendary);
        return (ContentTier)clampedValue;
    }

    public static int ToQualityValue(ContentTier tier)
    {
        return (int)tier;
    }

    private static ContentTier ClampAccessoryTier(ContentTier tier)
    {
        return (ContentTier)Mathf.Clamp((int)tier, (int)ContentTier.Common, (int)ContentTier.Legendary);
    }
}
