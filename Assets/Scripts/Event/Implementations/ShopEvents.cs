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
    public ShopOfferSnapshot Offer { get; }
    public int Price { get; }

    public ShopItemPurchasedEvent(Player player, ShopOfferSnapshot offer, int price)
    {
        Player = player;
        Offer = offer;
        Price = price;
    }
}

public readonly struct ShopItemLockedEvent
{
    public Player Player { get; }
    public ShopOfferSnapshot Offer { get; }

    public ShopItemLockedEvent(Player player, ShopOfferSnapshot offer)
    {
        Player = player;
        Offer = offer;
    }
}
