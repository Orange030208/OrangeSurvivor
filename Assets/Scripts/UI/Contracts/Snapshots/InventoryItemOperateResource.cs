public readonly struct InventoryItemOperateResource
{
    public readonly string entryId;
    public readonly ItemDataSO itemData;
    public readonly int colorDependencyNumber;
    public readonly int sellPrice;
    public readonly IDescribable describable;

    public InventoryItemOperateResource(
        string entryId,
        ItemDataSO itemData,
        int colorDependencyNumber,
        int sellPrice,
        IDescribable describable)
    {
        this.entryId = entryId;
        this.itemData = itemData;
        this.colorDependencyNumber = colorDependencyNumber;
        this.sellPrice = sellPrice;
        this.describable = describable;
    }
}
