public struct ShopItemData
{
    public ItemDataSO ItemData;
    public int Level;
}

public struct ShopItemsChangedEvent:IGameEvent
{
    public ShopItemData[] Items;
    public int RerollCost;

    public ShopItemsChangedEvent(ShopItemData[] items, int rerollCost)
    {
        Items = items;
        RerollCost = rerollCost;
    }
}

public struct ShopItemClickedEvent:IGameEvent
{
    public int ItemIndex;

    public ShopItemClickedEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct ShopRerollRequestedEvent:IGameEvent
{
}

public struct ShopVideoAdRerollRequestedEvent:IGameEvent
{
}

public struct RequestShopSnapshotEvent:IGameEvent
{
}

public struct ShopPurchaseFailedEvent:IGameEvent
{
    public string Message;

    public ShopPurchaseFailedEvent(string message)
    {
        Message = message;
    }
}

public struct ShopPurchaseSuccessEvent:IGameEvent
{
    public ItemDataSO ItemData;
    public int Level;

    public ShopPurchaseSuccessEvent(ItemDataSO itemData, int level)
    {
        ItemData = itemData;
        Level = level;
    }
}
