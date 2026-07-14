using UnityEngine;

public static class ShopPricingService
{
    public static int GetPrice(
        IShopProduct product,
        float runPriceMultiplier,
        float playerDiscountMultiplier,
        float statePriceMultiplier)
    {
        return product == null
            ? 0
            : ApplyPriceMultiplier(
                product.BasePrice,
                runPriceMultiplier,
                playerDiscountMultiplier,
                statePriceMultiplier);
    }

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
            playerDiscountMultiplier,
            1f);
    }

    public static int ApplyPriceMultiplier(
        int basePrice,
        float runPriceMultiplier,
        float playerDiscountMultiplier,
        float statePriceMultiplier = 1f)
    {
        float runMultiplier = runPriceMultiplier > 0f ? runPriceMultiplier : 1f;
        float discountMultiplier = PropValueUtility.ResolveEffectiveShopPriceMultiplier(playerDiscountMultiplier);
        float stateMultiplier = statePriceMultiplier > 0f ? statePriceMultiplier : 1f;
        return PropValueUtility.ResolveNonNegativePrice(basePrice * runMultiplier * discountMultiplier * stateMultiplier);
    }
}
