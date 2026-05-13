using UnityEngine;

public static class ShopPricingService
{
    public static int GetPrice(
        ItemDataSO itemData,
        int level,
        float contentPriceMultiplier,
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
            contentPriceMultiplier,
            runPriceMultiplier,
            playerDiscountMultiplier);
    }

    public static int ApplyPriceMultiplier(
        int basePrice,
        float contentPriceMultiplier,
        float runPriceMultiplier,
        float playerDiscountMultiplier)
    {
        float contentMultiplier = contentPriceMultiplier > 0f ? contentPriceMultiplier : 1f;
        float runMultiplier = runPriceMultiplier > 0f ? runPriceMultiplier : 1f;
        float discountMultiplier = PropValueUtility.ResolveEffectiveShopPriceMultiplier(playerDiscountMultiplier);
        return PropValueUtility.ResolveNonNegativePrice(basePrice * contentMultiplier * runMultiplier * discountMultiplier);
    }
}
