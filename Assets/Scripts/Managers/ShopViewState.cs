public readonly struct ShopViewState
{
    public ShopOfferViewData[] Offers { get; }
    public int RerollCost { get; }
    public int FreeRerollCount { get; }
    public bool CanReroll { get; }
    public bool IsRerollBlocked { get; }
    public ShopRefreshReason Reason { get; }

    public ShopViewState(ShopOfferViewData[] offers, int rerollCost, bool canReroll)
        : this(offers, rerollCost, 0, canReroll, false, ShopRefreshReason.StateUpdate)
    {
    }

    public ShopViewState(ShopOfferViewData[] offers, int rerollCost, bool canReroll, ShopRefreshReason reason)
        : this(offers, rerollCost, 0, canReroll, false, reason)
    {
    }

    public ShopViewState(
        ShopOfferViewData[] offers,
        int rerollCost,
        int freeRerollCount,
        bool canReroll,
        bool isRerollBlocked,
        ShopRefreshReason reason)
    {
        Offers = offers;
        RerollCost = rerollCost;
        FreeRerollCount = freeRerollCount;
        CanReroll = canReroll;
        IsRerollBlocked = isRerollBlocked;
        Reason = reason;
    }
}

public enum ShopRefreshReason
{
    StateUpdate,
    Initial,
    Purchase,
    Reroll,
    WaveRefresh
}
