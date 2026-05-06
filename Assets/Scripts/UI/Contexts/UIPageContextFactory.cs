using System;
using UnityEngine;

public static class UIPageContextFactory
{
    public static GamingPageContext CreateGamingPageContext(Player player)
    {
        EnsurePlayer(player);
        return new GamingPageContext(
            player,
            player.GetComponent<CurrencyWallet>());
    }

    public static ShopPageContext CreateShopPageContext(
        Player player,
        ShopManager shopManager,
        InventoryOperateManager inventoryOperateManager)
    {
        EnsurePlayer(player);
        CurrencyWallet currencyWallet = player.GetComponent<CurrencyWallet>();
        PropertiesManager propertiesManager = player.GetComponent<PropertiesManager>();

        EnsureShopManager(shopManager);
        BindInventoryManager(player, inventoryOperateManager);

        return new ShopPageContext(
            player,
            currencyWallet,
            propertiesManager,
            shopManager,
            inventoryOperateManager);
    }

    public static StageCompletePageContext CreateStageCompletePageContext(StageCompleteSummaryManager summaryManager)
    {
        if (summaryManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(StageCompleteSummaryManager)}.");
        }

        return new StageCompletePageContext(summaryManager.CreateSnapshot());
    }

    private static void BindInventoryManager(Player player, InventoryOperateManager inventoryOperateManager)
    {
        if (inventoryOperateManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(InventoryOperateManager)}.");
        }

        inventoryOperateManager.Bind(player);
    }

    private static void EnsureShopManager(ShopManager shopManager)
    {
        if (shopManager == null)
        {
            throw new MissingReferenceException($"{nameof(UIPageContextFactory)} requires an explicit {nameof(ShopManager)}.");
        }
    }

    private static void EnsurePlayer(Player player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player), $"{nameof(UIPageContextFactory)} requires an explicit {nameof(Player)}.");
        }
    }
}
