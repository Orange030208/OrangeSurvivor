using UnityEngine;

public static class UIPageContextFactory
{
    public static GamingPageContext CreateGamingPageContext(Player player = null, IInventoryUiFacade inventoryFacade = null)
    {
        Player resolvedPlayer = ResolvePlayer(player);
        bool ownsInventoryFacade = inventoryFacade == null;
        IInventoryUiFacade resolvedInventoryFacade = inventoryFacade ?? CreateInventoryFacade();
        return new GamingPageContext(
            resolvedPlayer,
            resolvedPlayer != null ? resolvedPlayer.GetComponent<CurrencyWallet>() : null,
            resolvedPlayer != null ? resolvedPlayer.GetComponent<PropertiesManager>() : null,
            resolvedInventoryFacade,
            ownsInventoryFacade);
    }

    public static PauseMenuContext CreatePauseMenuContext(Player player = null, IInventoryUiFacade inventoryFacade = null)
    {
        Player resolvedPlayer = ResolvePlayer(player);
        bool ownsInventoryFacade = inventoryFacade == null;
        IInventoryUiFacade resolvedInventoryFacade = inventoryFacade ?? CreateInventoryFacade();
        return new PauseMenuContext(
            resolvedPlayer,
            resolvedPlayer != null ? resolvedPlayer.GetComponent<CurrencyWallet>() : null,
            resolvedPlayer != null ? resolvedPlayer.GetComponent<PropertiesManager>() : null,
            resolvedInventoryFacade,
            ownsInventoryFacade);
    }

    public static ShopPageContext CreateShopPageContext(Player player = null, IShopUiFacade shopFacade = null, IInventoryUiFacade inventoryFacade = null)
    {
        Player resolvedPlayer = ResolvePlayer(player);
        CurrencyWallet currencyWallet = resolvedPlayer != null ? resolvedPlayer.GetComponent<CurrencyWallet>() : null;
        PropertiesManager propertiesManager = resolvedPlayer != null ? resolvedPlayer.GetComponent<PropertiesManager>() : null;

        bool ownsShopFacade = shopFacade == null;
        IShopUiFacade resolvedShopFacade = shopFacade ?? new EventBusShopUiFacade(currencyWallet);
        bool ownsInventoryFacade = inventoryFacade == null;
        IInventoryUiFacade resolvedInventoryFacade = inventoryFacade ?? CreateInventoryFacade();

        return new ShopPageContext(
            resolvedPlayer,
            currencyWallet,
            propertiesManager,
            resolvedShopFacade,
            ownsShopFacade,
            resolvedInventoryFacade,
            ownsInventoryFacade);
    }

    private static Player ResolvePlayer(Player player)
    {
        return player != null ? player : Object.FindFirstObjectByType<Player>();
    }

    private static IInventoryUiFacade CreateInventoryFacade()
    {
        InventoryOperateManager inventoryOperateManager = Object.FindFirstObjectByType<InventoryOperateManager>();
        if (inventoryOperateManager != null)
        {
            return new ManagerInventoryUiFacade(inventoryOperateManager);
        }

        return new EventBusInventoryUiFacade();
    }
}
