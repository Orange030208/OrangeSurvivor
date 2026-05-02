public readonly struct ShopSnapshot
{
    public ShopItemData[] Items { get; }
    public int RerollCost { get; }
    public bool CanReroll { get; }
    public ShopSnapshotReason Reason { get; }

    public ShopSnapshot(ShopItemData[] items, int rerollCost, bool canReroll)
        : this(items, rerollCost, canReroll, ShopSnapshotReason.StateUpdate)
    {
    }

    public ShopSnapshot(ShopItemData[] items, int rerollCost, bool canReroll, ShopSnapshotReason reason)
    {
        Items = items;
        RerollCost = rerollCost;
        CanReroll = canReroll;
        Reason = reason;
    }
}

public enum ShopSnapshotReason
{
    StateUpdate,
    Initial,
    Purchase,
    Reroll,
    WaveRefresh
}
