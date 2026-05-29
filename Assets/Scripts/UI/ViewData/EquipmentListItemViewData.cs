public readonly struct EquipmentListItemViewData
{
    public readonly string EntryId;
    public readonly ItemDataSO ItemData;
    public readonly IHasContentTier TierSource;

    public EquipmentListItemViewData(string entryId, ItemDataSO itemData, IHasContentTier tierSource)
    {
        EntryId = entryId;
        ItemData = itemData;
        TierSource = tierSource;
    }
}
