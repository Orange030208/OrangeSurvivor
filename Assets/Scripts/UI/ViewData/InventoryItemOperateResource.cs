public readonly struct InventoryItemOperateResource
{
    public readonly string entryId;
    public readonly ItemDataSO itemData;
    public readonly int colorDependencyNumber;
    public readonly int sellPrice;
    public readonly object infoSource;

    public InventoryItemOperateResource(
        string entryId,
        ItemDataSO itemData,
        int colorDependencyNumber,
        int sellPrice,
        object infoSource)
    {
        this.entryId = entryId;
        this.itemData = itemData;
        this.colorDependencyNumber = colorDependencyNumber;
        this.sellPrice = sellPrice;
        this.infoSource = infoSource;
    }
}
