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

public enum CardQuality
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

public static class ContentTierResolver
{
    public static ContentTier FromUpgradeCardRarity(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Rare => ContentTier.Rare,
            UpgradeCardRarity.Epic => ContentTier.Epic,
            UpgradeCardRarity.Legendary => ContentTier.Legendary,
            _ => ContentTier.Common
        };
    }

    public static ContentTier FromAccessoryRarity(AccessoryRarity rarity)
    {
        return rarity switch
        {
            AccessoryRarity.Rare => ContentTier.Rare,
            AccessoryRarity.Epic => ContentTier.Epic,
            AccessoryRarity.Legendary => ContentTier.Legendary,
            _ => ContentTier.Common
        };
    }

    public static ContentTier FromAccessoryRarity(int rarity)
    {
        int clampedRarity = Mathf.Clamp(rarity, (int)AccessoryRarity.Common, (int)AccessoryRarity.Legendary);
        return FromAccessoryRarity((AccessoryRarity)clampedRarity);
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
            ItemType.Accessory => FromAccessoryRarity(sourceValue),
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
}

public static class ContentTierPresentationUtility
{
    public static CardQuality ToCardQuality(this ContentTier tier)
    {
        return (CardQuality)Mathf.Clamp((int)tier, (int)CardQuality.Common, (int)CardQuality.Legendary);
    }

    public static ContentTier ToContentTier(this CardQuality quality)
    {
        return (ContentTier)Mathf.Clamp((int)quality, (int)ContentTier.Common, (int)ContentTier.Legendary);
    }
}
