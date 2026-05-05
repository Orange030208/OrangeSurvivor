using System;
using UnityEngine;

public static class UIPageContextFactory
{
    public static GamingPageContext CreateGamingPageContext(
        Player player,
        InventoryOperateManager inventoryOperateManager)
    {
        EnsurePlayer(player);
        IInventoryUiFacade inventoryFacade = CreateInventoryFacade(player, inventoryOperateManager);
        return new GamingPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<PropertiesManager>(),
            inventoryFacade,
            true);
    }

    public static PauseMenuContext CreatePauseMenuContext(
        Player player,
        InventoryOperateManager inventoryOperateManager)
    {
        EnsurePlayer(player);
        IInventoryUiFacade inventoryFacade = CreateInventoryFacade(player, inventoryOperateManager);
        return new PauseMenuContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<PropertiesManager>(),
            inventoryFacade,
            true);
    }

    public static ShopPageContext CreateShopPageContext(
        Player player,
        ShopManager shopManager,
        InventoryOperateManager inventoryOperateManager)
    {
        EnsurePlayer(player);
        CurrencyWallet currencyWallet = player.GetComponent<CurrencyWallet>();
        PropertiesManager propertiesManager = player.GetComponent<PropertiesManager>();

        IShopUiFacade shopFacade = CreateShopFacade(shopManager, currencyWallet);
        IInventoryUiFacade inventoryFacade = CreateInventoryFacade(player, inventoryOperateManager);

        return new ShopPageContext(
            player,
            currencyWallet,
            propertiesManager,
            shopFacade,
            true,
            inventoryFacade,
            true);
    }

    public static StageCompletePageContext CreateStageCompletePageContext(StageCompleteSummaryManager summaryManager)
    {
        if (summaryManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(StageCompleteSummaryManager)}.");
        }

        return new StageCompletePageContext(summaryManager.CreateSnapshot());
    }

    private static IInventoryUiFacade CreateInventoryFacade(Player player, InventoryOperateManager inventoryOperateManager)
    {
        if (inventoryOperateManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(InventoryOperateManager)}.");
        }

        inventoryOperateManager.Bind(player);
        return new ManagerInventoryUiFacade(inventoryOperateManager);
    }

    private static IShopUiFacade CreateShopFacade(ShopManager shopManager, CurrencyWallet currencyWallet)
    {
        if (shopManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(ShopManager)}.");
        }

        return new ManagerShopUiFacade(shopManager, currencyWallet);
    }

    private static void EnsurePlayer(Player player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player), $"{nameof(UIPageContextFactory)} requires an explicit {nameof(Player)}.");
        }
    }
}
