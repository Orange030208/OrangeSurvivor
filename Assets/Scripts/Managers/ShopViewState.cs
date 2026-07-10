public readonly struct ShopViewState
{
    public ShopItemData[] Items { get; }
    public int RerollCost { get; }
    public int FreeRerollCount { get; }
    public bool CanReroll { get; }
    public ShopRefreshReason Reason { get; }

    public ShopViewState(ShopItemData[] items, int rerollCost, bool canReroll)
        : this(items, rerollCost, 0, canReroll, ShopRefreshReason.StateUpdate)
    {
    }

    public ShopViewState(ShopItemData[] items, int rerollCost, bool canReroll, ShopRefreshReason reason)
        : this(items, rerollCost, 0, canReroll, reason)
    {
    }

    public ShopViewState(ShopItemData[] items, int rerollCost, int freeRerollCount, bool canReroll, ShopRefreshReason reason)
    {
        Items = items;
        RerollCost = rerollCost;
        FreeRerollCount = freeRerollCount;
        CanReroll = canReroll;
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
