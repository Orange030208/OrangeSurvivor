using System;

public sealed class ShopPageContext : IPageContext, IInventoryFacadeContext
{
    private readonly bool disposeShopFacadeOnDispose;
    private readonly bool disposeInventoryFacadeOnDispose;

    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        IShopUiFacade shopFacade,
        bool disposeShopFacadeOnDispose,
        IInventoryUiFacade inventoryFacade,
        bool disposeInventoryFacadeOnDispose)
    {
        Player = player;
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        ShopFacade = shopFacade ?? throw new ArgumentNullException(nameof(shopFacade));
        InventoryFacade = inventoryFacade ?? throw new ArgumentNullException(nameof(inventoryFacade));
        this.disposeShopFacadeOnDispose = disposeShopFacadeOnDispose;
        this.disposeInventoryFacadeOnDispose = disposeInventoryFacadeOnDispose;
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public IShopUiFacade ShopFacade { get; }
    public IInventoryUiFacade InventoryFacade { get; }

    public void Dispose()
    {
        if (disposeShopFacadeOnDispose)
        {
            ShopFacade.Dispose();
        }

        if (!disposeInventoryFacadeOnDispose)
        {
            return;
        }

        InventoryFacade.Dispose();
    }
}
