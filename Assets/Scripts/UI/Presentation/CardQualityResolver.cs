using UnityEngine;

public static class CardQualityResolver
{
    public static CardQuality FromUpgradeCardRarity(UpgradeCardRarity rarity)
    {
        return ContentTierResolver.FromUpgradeCardRarity(rarity).ToCardQuality();
    }

    public static CardQuality FromAccessoryRarity(AccessoryRarity rarity)
    {
        return ContentTierResolver.FromAccessoryRarity(rarity).ToCardQuality();
    }

    public static CardQuality FromAccessoryRarity(int rarity)
    {
        return ContentTierResolver.FromAccessoryRarity(rarity).ToCardQuality();
    }

    public static CardQuality FromWeaponLevel(int level)
    {
        return ContentTierResolver.FromWeaponLevel(level).ToCardQuality();
    }

    public static CardQuality FromItem(ItemDataSO itemData, int qualityValue)
    {
        return ContentTierResolver.FromItem(itemData, qualityValue).ToCardQuality();
    }
}
