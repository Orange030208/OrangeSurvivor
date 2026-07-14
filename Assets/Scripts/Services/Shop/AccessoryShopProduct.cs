using System;

/// <summary>
/// 商店中的饰品商品。饰品没有商品等级，不需要填充无意义字段。
/// </summary>
public sealed class AccessoryShopProduct : IShopProduct
{
    public AccessoryShopProduct(AccessoryDataSO accessoryData)
    {
        AccessoryData = accessoryData ?? throw new ArgumentNullException(nameof(accessoryData));
        Key = ShopProductKey.CreateAccessory(AccessoryData);
    }

    public AccessoryDataSO AccessoryData { get; }
    public ShopProductKey Key { get; }
    public ItemDataSO DisplayItem => AccessoryData;
    public string DisplayName => ItemNameStyleUtility.GetAccessoryDisplayName(AccessoryData.ItemName, AccessoryData.Tier);
    public string TypeText => "饰品";
    public ContentTier Tier => AccessoryData.Tier;
    public int BasePrice => AccessoryData.ItemPrice;

    public InfoDocument BuildInfoDocument()
    {
        return AccessoryData.BuildInfoDocument();
    }

    public ShopPurchaseResult TryPurchase(ShopPurchaseContext context)
    {
        if (context.AccessoryManager == null)
        {
            return ShopPurchaseResult.Failure("Accessory manager not found.");
        }

        return context.AccessoryManager.EquipAccessory(AccessoryData, false)
            ? ShopPurchaseResult.Success()
            : ShopPurchaseResult.Failure("Accessory owned limit reached.");
    }
}
