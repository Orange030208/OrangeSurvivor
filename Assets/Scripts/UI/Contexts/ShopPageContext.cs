using System;

public sealed class ShopPageContext
{
    public ShopPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        AttributeManager attributeManager,
        ShopManager shopManager)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
        AttributeManager = attributeManager;
        ShopManager = shopManager ?? throw new ArgumentNullException(nameof(shopManager));
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public AttributeManager AttributeManager { get; }
    public ShopManager ShopManager { get; }
}
