using UnityEngine;

public struct ShopItemData : IHasContentTier
{
    public ItemDataSO ItemData;
    public int Level;
    public bool Lock;
    public bool SoldOut;
    public float RunPriceMultiplier;
    public float PlayerDiscountMultiplier;
    public ContentTier Tier => ResolveTier();

    public int GetPrice()
    {
        return ShopPricingService.GetPrice(
            ItemData,
            Level,
            RunPriceMultiplier,
            PlayerDiscountMultiplier);
    }

    private ContentTier ResolveTier()
    {
        if (ItemData == null)
        {
            return ContentTier.Common;
        }

        if (ItemData.ItemType == ItemType.Weapon)
        {
            return ContentTierResolver.FromWeaponLevel(Level);
        }

        if (ItemData is AccessoryDataSO accessoryData)
        {
            return accessoryData.Tier;
        }

        return ContentTier.Common;
    }
}
