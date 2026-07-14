/// <summary>
/// 货架对外事件快照。事件只暴露已确定的商品信息，不暴露可变运行时状态对象。
/// </summary>
public readonly struct ShopOfferSnapshot
{
    public ShopOfferSnapshot(
        int offerId,
        int slotIndex,
        ShopProductKey productKey,
        ItemDataSO displayItem,
        string displayName,
        ContentTier tier,
        bool wasLockedLastVisit,
        bool isLocked,
        bool isSoldOut)
    {
        OfferId = offerId;
        SlotIndex = slotIndex;
        ProductKey = productKey;
        DisplayItem = displayItem;
        DisplayName = displayName ?? string.Empty;
        Tier = tier;
        WasLockedLastVisit = wasLockedLastVisit;
        IsLocked = isLocked;
        IsSoldOut = isSoldOut;
    }

    public int OfferId { get; }
    public int SlotIndex { get; }
    public ShopProductKey ProductKey { get; }
    public ItemDataSO DisplayItem { get; }
    public string DisplayName { get; }
    public ContentTier Tier { get; }
    public bool WasLockedLastVisit { get; }
    public bool IsLocked { get; }
    public bool IsSoldOut { get; }
}
