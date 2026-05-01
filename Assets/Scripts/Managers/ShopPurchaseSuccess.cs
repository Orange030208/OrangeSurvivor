public readonly struct ShopPurchaseSuccess
{
    public ItemDataSO ItemData { get; }
    public int Level { get; }

    public ShopPurchaseSuccess(ItemDataSO itemData, int level)
    {
        ItemData = itemData;
        Level = level;
    }
}
