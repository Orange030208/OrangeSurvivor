using UnityEngine;

public static class ShopPricingService
{
    public static int GetPrice(
        ItemDataSO itemData,
        int level,
        float runPriceMultiplier,
        float playerDiscountMultiplier)
    {
        if (itemData == null)
        {
            return 0;
        }

        int basePrice = itemData.ItemType == ItemType.Weapon
            ? WeaponPriceHelper.GetPrice(itemData.ItemPrice, level)
            : itemData.ItemPrice;
        return ApplyPriceMultiplier(
            basePrice,
            runPriceMultiplier,
            playerDiscountMultiplier);
    }

    public static int ApplyPriceMultiplier(
        int basePrice,
        float runPriceMultiplier,
        float playerDiscountMultiplier)
    {
        float runMultiplier = runPriceMultiplier > 0f ? runPriceMultiplier : 1f;
        float discountMultiplier = PropValueUtility.ResolveEffectiveShopPriceMultiplier(playerDiscountMultiplier);
        return PropValueUtility.ResolveNonNegativePrice(basePrice * runMultiplier * discountMultiplier);
    }
}
