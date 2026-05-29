using System;

public sealed class ShopPageContext
{
    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        ShopManager shopManager)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        ShopManager = shopManager ?? throw new ArgumentNullException(nameof(shopManager));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public ShopManager ShopManager { get; }
}
