using System;

public struct ShopItemData : IGameEvent
{
    public int Index;
    public ItemDataSO ItemData;
    public int Level;

    public ItemType ItemType => ItemData?.ItemType ?? ItemType.Weapon;

    public static ShopItemData CreateAccessory(int index, AccessoryDataSO accessoryData)
    {
        return new ShopItemData
        {
            Index = index,
            ItemData = accessoryData,
            Level = 1
        };
    }

    public static ShopItemData CreateWeapon(int index, WeaponDataSO weaponData, int level)
    {
        return new ShopItemData
        {
            Index = index,
            ItemData = weaponData,
            Level = level
        };
    }
}

public struct ShopItemsChangedEvent : IGameEvent
{
    public ShopItemData[] Items;
    public int RerollCost;

    public ShopItemsChangedEvent(ShopItemData[] items, int rerollCost)
    {
        Items = items;
        RerollCost = rerollCost;
    }
}

public struct ShopItemClickedEvent : IGameEvent
{
    public int ItemIndex;
    public ShopItemData ItemData;

    public ShopItemClickedEvent(int itemIndex, ShopItemData itemData)
    {
        ItemIndex = itemIndex;
        ItemData = itemData;
    }
}

public struct ShopRerollRequestedEvent : IGameEvent
{
}

public struct ShopVideoAdRerollRequestedEvent : IGameEvent
{
}

public struct RequestShopSnapshotEvent : IGameEvent
{
}

public struct ShopPurchaseFailedEvent : IGameEvent
{
    public string Message;

    public ShopPurchaseFailedEvent(string message)
    {
        Message = message;
    }
}

public struct ShopPurchaseSuccessEvent : IGameEvent
{
    public ShopItemData PurchasedItem;

    public ShopPurchaseSuccessEvent(ShopItemData purchasedItem)
    {
        PurchasedItem = purchasedItem;
    }
}
