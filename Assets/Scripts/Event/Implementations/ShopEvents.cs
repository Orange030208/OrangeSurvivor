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
