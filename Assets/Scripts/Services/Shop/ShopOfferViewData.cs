using UnityEngine;

/// <summary>
/// UI 使用的货架快照。表现层不需要知道武器等级或具体购买逻辑。
/// </summary>
public readonly struct ShopOfferViewData : IHasContentTier
{
    public ShopOfferViewData(
        int offerId,
        int slotIndex,
        ShopProductKey productKey,
        ItemDataSO displayItem,
        string displayName,
        string typeText,
        Sprite icon,
        ContentTier tier,
        int price,
        int originalPrice,
        bool wasLockedLastVisit,
        bool isLocked,
        bool isSoldOut,
        InfoDocument infoDocument)
    {
        OfferId = offerId;
        SlotIndex = slotIndex;
        ProductKey = productKey;
        DisplayItem = displayItem;
        DisplayName = displayName ?? string.Empty;
        TypeText = typeText ?? string.Empty;
        Icon = icon;
        Tier = tier;
        Price = price;
        OriginalPrice = originalPrice;
        WasLockedLastVisit = wasLockedLastVisit;
        IsLocked = isLocked;
        IsSoldOut = isSoldOut;
        InfoDocument = infoDocument;
    }

    public int OfferId { get; }
    public int SlotIndex { get; }
    public ShopProductKey ProductKey { get; }
    public ItemDataSO DisplayItem { get; }
    public string DisplayName { get; }
    public string TypeText { get; }
    public Sprite Icon { get; }
    public ContentTier Tier { get; }
    public int Price { get; }
    public int OriginalPrice { get; }
    public bool WasLockedLastVisit { get; }
    public bool IsLocked { get; }
    public bool IsSoldOut { get; }
    public InfoDocument InfoDocument { get; }
}
