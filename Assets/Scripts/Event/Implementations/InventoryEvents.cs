public readonly struct InventoryUIItemSnapshot
{
    public readonly ItemDataSO ItemData;
    public readonly int ColorDependencyNumber;

    public InventoryUIItemSnapshot(ItemDataSO itemData, int colorDependencyNumber)
    {
        ItemData = itemData;
        ColorDependencyNumber = colorDependencyNumber;
    }
}

public struct RequestInventorySnapshotEvent : IGameEvent
{
}

public struct InventorySnapshotChangedEvent : IGameEvent
{
    public InventoryUIItemSnapshot[] Items;

    public InventorySnapshotChangedEvent(InventoryUIItemSnapshot[] items)
    {
        Items = items;
    }
}

public struct InventoryItemClickedEvent : IGameEvent
{
    public int ItemIndex;

    public InventoryItemClickedEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct RequestInventoryItemOperatePanelEvent : IGameEvent
{
    public int ItemIndex;

    public RequestInventoryItemOperatePanelEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct InventoryItemOperatePanelDataEvent : IGameEvent
{
    public InventoryItemOperateResource Resource;

    public InventoryItemOperatePanelDataEvent(InventoryItemOperateResource resource)
    {
        Resource = resource;
    }
}

public struct InventoryItemSellClickedEvent : IGameEvent
{
    public int ItemIndex;

    public InventoryItemSellClickedEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct InventoryItemMergeClickedEvent : IGameEvent
{
    public int ItemIndex;

    public InventoryItemMergeClickedEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct InventoryItemOperatePanelCloseClickedEvent : IGameEvent
{
    public int ItemIndex;

    public InventoryItemOperatePanelCloseClickedEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}

public struct InventoryItemOperatePanelShouldCloseEvent : IGameEvent
{
    public int ItemIndex;

    public InventoryItemOperatePanelShouldCloseEvent(int itemIndex)
    {
        ItemIndex = itemIndex;
    }
}
