using System;

public sealed class GamingPageContext
{
    public GamingPageContext(
        Player player,
        CurrencyWallet currencyWallet,
        WaveHudViewData waveHudViewData)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        CurrencyWallet = currencyWallet;
        WaveHudViewData = waveHudViewData;
    }

    public Player Player { get; }
    public CurrencyWallet CurrencyWallet { get; }
    public WaveHudViewData WaveHudViewData { get; }
}
