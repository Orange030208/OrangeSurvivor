using System;

/// <summary>
/// 商店中的武器商品。武器等级是武器商品自己的规格，不再泄漏成通用货架字段。
/// </summary>
public sealed class WeaponShopProduct : IShopProduct
{
    public WeaponShopProduct(WeaponDataSO weaponData, int weaponLevel)
    {
        WeaponData = weaponData ?? throw new ArgumentNullException(nameof(weaponData));
        WeaponLevel = WeaponLevelHelper.ClampLevel(weaponLevel);
        Key = ShopProductKey.CreateWeapon(WeaponData, WeaponLevel);
    }

    public WeaponDataSO WeaponData { get; }
    public int WeaponLevel { get; }
    public ShopProductKey Key { get; }
    public ItemDataSO DisplayItem => WeaponData;
    public string DisplayName => ItemNameStyleUtility.GetWeaponDisplayName(WeaponData.ItemName, WeaponLevel);
    public string TypeText => "武器";
    public ContentTier Tier => ContentTierResolver.FromWeaponLevel(WeaponLevel);
    public int BasePrice => WeaponPriceHelper.GetPrice(WeaponData.ItemPrice, WeaponLevel);

    public InfoDocument BuildInfoDocument()
    {
        return new WeaponLevelDescribable(WeaponData, WeaponLevel).BuildInfoDocument();
    }

    public ShopPurchaseResult TryPurchase(ShopPurchaseContext context)
    {
        if (context.WeaponsHolder == null)
        {
            return ShopPurchaseResult.Failure("Weapons holder not found.");
        }

        return context.WeaponsHolder.AddWeapon(WeaponData, WeaponLevel, false)
            ? ShopPurchaseResult.Success()
            : ShopPurchaseResult.Failure("No empty weapon slot available.");
    }
}
