using System;

public sealed class GamingPageContext
{
    public GamingPageContext(
        Player player,
        CurrencyWallet currencyWallet)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
}
