public readonly struct InventoryItemViewData
{
    public readonly string EntryId;
    public readonly ItemDataSO ItemData;
    public readonly ContentTier Tier;

    public InventoryItemViewData(string entryId, ItemDataSO itemData, ContentTier tier)
    {
        EntryId = entryId;
        ItemData = itemData;
        Tier = tier;
    }
}
