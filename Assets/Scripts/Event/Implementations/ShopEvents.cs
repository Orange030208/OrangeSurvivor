public struct ShopFreeRerollsGrantedEvent
{
    public Player Player;
    public int Count;

    public ShopFreeRerollsGrantedEvent(Player player, int count)
    {
        Player = player;
        Count = count;
    }
}

public readonly struct ShopRerolledEvent
{
    public Player Player { get; }
    public bool UsedFreeReroll { get; }

    public ShopRerolledEvent(Player player, bool usedFreeReroll)
    {
        Player = player;
        UsedFreeReroll = usedFreeReroll;
    }
}

public readonly struct ShopItemPurchasedEvent
{
    public Player Player { get; }
    public ItemDataSO ItemData { get; }
    public int Level { get; }
    public int Price { get; }

    public ShopItemPurchasedEvent(Player player, ItemDataSO itemData, int level, int price)
    {
        Player = player;
        ItemData = itemData;
        Level = level;
        Price = price;
    }
}

public readonly struct ShopItemLockedEvent
{
    public Player Player { get; }
    public int ItemIndex { get; }
    public ItemDataSO ItemData { get; }
    public int Level { get; }

    public ShopItemLockedEvent(Player player, int itemIndex, ItemDataSO itemData, int level)
    {
        Player = player;
        ItemIndex = itemIndex;
        ItemData = itemData;
        Level = level;
    }
}
