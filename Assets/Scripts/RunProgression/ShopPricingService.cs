using UnityEngine;

public static class ShopPricingService
{
    public static int GetPrice(
        IShopProduct product,
        float runPriceMultiplier,
        float playerDiscountMultiplier,
        float globalPriceModifierMultiplier = 1f,
        float productPriceModifierMultiplier = 1f)
    {
        return product == null
            ? 0
            : ApplyPriceMultiplier(
                product.BasePrice,
                runPriceMultiplier,
                playerDiscountMultiplier,
                globalPriceModifierMultiplier,
                productPriceModifierMultiplier);
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
            playerDiscountMultiplier);
    }

    public static int ApplyPriceMultiplier(
        int basePrice,
        float runPriceMultiplier,
        float playerDiscountMultiplier,
        float globalPriceModifierMultiplier = 1f,
        float productPriceModifierMultiplier = 1f)
    {
        float runMultiplier = runPriceMultiplier > 0f ? runPriceMultiplier : 1f;
        float discountMultiplier = PropValueUtility.ResolveEffectiveShopPriceMultiplier(playerDiscountMultiplier);
        float globalMultiplier = Mathf.Max(0f, globalPriceModifierMultiplier);
        float productMultiplier = Mathf.Max(0f, productPriceModifierMultiplier);
        return PropValueUtility.ResolveNonNegativePrice(
            basePrice * runMultiplier * discountMultiplier * globalMultiplier * productMultiplier);
    }
}
