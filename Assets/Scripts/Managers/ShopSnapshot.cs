public readonly struct ShopSnapshot
{
    public ShopItemData[] Items { get; }
    public int RerollCost { get; }
    public bool CanReroll { get; }

    public ShopSnapshot(ShopItemData[] items, int rerollCost, bool canReroll)
    {
        Items = items;
        RerollCost = rerollCost;
        CanReroll = canReroll;
    }
}
