/// <summary>
/// 商品执行购买时需要的玩家侧依赖。
/// </summary>
public readonly struct ShopPurchaseContext
{
    public ShopPurchaseContext(
        Player player,
        WeaponsHolder weaponsHolder,
        AccessoryManager accessoryManager,
        CurrencyWallet currencyWallet)
    {
        Player = player;
        WeaponsHolder = weaponsHolder;
        AccessoryManager = accessoryManager;
        CurrencyWallet = currencyWallet;
    }

    public Player Player { get; }
    public WeaponsHolder WeaponsHolder { get; }
    public AccessoryManager AccessoryManager { get; }
    public CurrencyWallet CurrencyWallet { get; }
}
