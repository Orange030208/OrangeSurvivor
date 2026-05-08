public readonly struct ShopViewState
{
    public ShopItemData[] Items { get; }
    public int RerollCost { get; }
    public bool CanReroll { get; }
    public ShopRefreshReason Reason { get; }

    public ShopViewState(ShopItemData[] items, int rerollCost, bool canReroll)
        : this(items, rerollCost, canReroll, ShopRefreshReason.StateUpdate)
    {
    }

    public ShopViewState(ShopItemData[] items, int rerollCost, bool canReroll, ShopRefreshReason reason)
    {
        Items = items;
        RerollCost = rerollCost;
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
