using System;

public sealed class ShopPageContext
{
    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        ShopManager shopManager,
        InventoryOperateManager inventoryOperateManager)
    {
        Player = player;
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        ShopManager = shopManager ?? throw new ArgumentNullException(nameof(shopManager));
        InventoryOperateManager = inventoryOperateManager
            ?? throw new ArgumentNullException(nameof(inventoryOperateManager));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public ShopManager ShopManager { get; }
    public InventoryOperateManager InventoryOperateManager { get; }
}
