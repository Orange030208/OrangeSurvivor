/// <summary>
/// 商店商品规格。它只描述“卖的是什么”以及“如何购买”，不保存货架状态。
/// </summary>
public interface IShopProduct : IHasContentTier
{
    ShopProductKey Key { get; }
    ItemDataSO DisplayItem { get; }
    string DisplayName { get; }
    string TypeText { get; }
    int BasePrice { get; }

    InfoDocument BuildInfoDocument();
    ShopPurchaseResult TryPurchase(Player player);
}
