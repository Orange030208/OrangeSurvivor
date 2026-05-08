public readonly struct InventoryItemViewData
{
    public readonly string EntryId;
    public readonly ItemDataSO ItemData;
    public readonly int ColorDependencyNumber;

    public InventoryItemViewData(string entryId, ItemDataSO itemData, int colorDependencyNumber)
    {
        EntryId = entryId;
        ItemData = itemData;
        ColorDependencyNumber = colorDependencyNumber;
    }
}
