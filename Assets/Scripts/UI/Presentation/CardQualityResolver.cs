using UnityEngine;

public static class CardQualityResolver
{
    public static CardQuality FromUpgradeCardRarity(UpgradeCardRarity rarity)
    {
        return ContentTierResolver.FromUpgradeCardRarity(rarity).ToCardQuality();
    }

    public static CardQuality FromAccessoryTier(ContentTier tier)
    {
        return tier.ToCardQuality();
    }

    public static CardQuality FromAccessoryTier(int tier)
    {
        return ((ContentTier)tier).ToCardQuality();
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
