public struct ShopVideoAdRerollRequestedEvent : IGameEvent
{
}

public struct ShopContinueClickedEvent : IGameEvent
{
}

public struct ShopFreeRerollsGrantedEvent : IGameEvent
{
    public Player Player;
    public int Count;

    public ShopFreeRerollsGrantedEvent(Player player, int count)
    {
        Player = player;
        Count = count;
    }
}
