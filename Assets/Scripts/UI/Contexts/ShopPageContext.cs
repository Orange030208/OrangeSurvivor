using System;

public sealed class ShopPageContext
{
    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        PropertiesManager propertiesManager,
        IShopController shopController)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
        PropertiesManager = propertiesManager;
        ShopController = shopController ?? throw new ArgumentNullException(nameof(shopController));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public PropertiesManager PropertiesManager { get; }
    public IShopController ShopController { get; }
}
