using System;

public sealed class ShopPageContext
{
    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        AttributeManager attributeManager,
        IShopController shopController)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
        AttributeManager = attributeManager;
        ShopController = shopController ?? throw new ArgumentNullException(nameof(shopController));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public AttributeManager AttributeManager { get; }
    public IShopController ShopController { get; }
}
