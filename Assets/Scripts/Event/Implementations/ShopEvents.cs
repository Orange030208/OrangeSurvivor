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
