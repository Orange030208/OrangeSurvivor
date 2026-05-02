using UnityEngine;

public static class CardQualityResolver
{
    public static CardQuality FromUpgradeCardRarity(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Rare => CardQuality.Rare,
            UpgradeCardRarity.Epic => CardQuality.Epic,
            UpgradeCardRarity.Legendary => CardQuality.Legendary,
            _ => CardQuality.Common
        };
    }

    public static CardQuality FromAccessoryRarity(AccessoryRarity rarity)
    {
        return rarity switch
        {
            AccessoryRarity.Rare => CardQuality.Rare,
            AccessoryRarity.Epic => CardQuality.Epic,
            AccessoryRarity.Legendary => CardQuality.Legendary,
            _ => CardQuality.Common
        };
    }

    public static CardQuality FromAccessoryRarity(int rarity)
    {
        int clampedRarity = Mathf.Clamp(rarity, (int)AccessoryRarity.Common, (int)AccessoryRarity.Legendary);
        return FromAccessoryRarity((AccessoryRarity)clampedRarity);
    }

    public static CardQuality FromWeaponLevel(int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        return clampedLevel switch
        {
            2 => CardQuality.Rare,
            3 => CardQuality.Epic,
            4 => CardQuality.Legendary,
            _ => CardQuality.Common
        };
    }

    public static CardQuality FromItem(ItemDataSO itemData, int qualityValue)
    {
        if (itemData == null)
        {
            return CardQuality.Common;
        }

        return itemData.ItemType switch
        {
            ItemType.Accessory => FromAccessoryRarity(qualityValue),
            ItemType.Weapon => FromWeaponLevel(qualityValue),
            _ => CardQuality.Common
        };
    }
}

