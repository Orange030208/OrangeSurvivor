using System;

public sealed class PauseMenuContext
{
    public PauseMenuContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        InventoryOperateManager inventoryOperateManager)
    {
        Player = player;
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        InventoryOperateManager = inventoryOperateManager
            ?? throw new ArgumentNullException(nameof(inventoryOperateManager));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public InventoryOperateManager InventoryOperateManager { get; }
}
