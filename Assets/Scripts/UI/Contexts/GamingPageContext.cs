using System;

public sealed class GamingPageContext : IPageContext, IInventoryFacadeContext
{
    private readonly bool disposeInventoryFacadeOnDispose;

    public GamingPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        IInventoryUiFacade inventoryFacade,
        bool disposeInventoryFacadeOnDispose)
    {
        Player = player;
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        InventoryFacade = inventoryFacade ?? throw new ArgumentNullException(nameof(inventoryFacade));
        this.disposeInventoryFacadeOnDispose = disposeInventoryFacadeOnDispose;
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public IInventoryUiFacade InventoryFacade { get; }

    public void Dispose()
    {
        if (!disposeInventoryFacadeOnDispose)
        {
            return;
        }

        InventoryFacade.Dispose();
    }
}
