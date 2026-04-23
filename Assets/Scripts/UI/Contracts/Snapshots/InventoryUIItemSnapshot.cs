public readonly struct InventoryUIItemSnapshot
{
    public readonly string EntryId;
    public readonly ItemDataSO ItemData;
    public readonly int ColorDependencyNumber;

    public InventoryUIItemSnapshot(string entryId, ItemDataSO itemData, int colorDependencyNumber)
    {
        EntryId = entryId;
        ItemData = itemData;
        ColorDependencyNumber = colorDependencyNumber;
    }
}
