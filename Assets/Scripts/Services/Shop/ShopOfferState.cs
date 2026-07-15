using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个货架格子的运行时状态。它是商品在商店里的唯一可信状态源。
/// </summary>
public sealed class ShopOfferState
{
    private readonly Dictionary<string, float> priceModifiers = new();

    public ShopOfferState(int offerId, IShopProduct product)
    {
        OfferId = offerId;
        Product = product ?? throw new ArgumentNullException(nameof(product));
    }

    public int OfferId { get; }
    public int SlotIndex { get; private set; }
    public IShopProduct Product { get; }
    public bool WasLockedLastVisit { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsSoldOut { get; private set; }

    public void SetSlotIndex(int slotIndex)
    {
        SlotIndex = Mathf.Max(0, slotIndex);
    }

    public void MarkLockedStateAsPreviousVisit()
    {
        WasLockedLastVisit = IsLocked;
    }

    public void SetLocked(bool locked)
    {
        if (IsSoldOut)
        {
            IsLocked = false;
            return;
        }

        IsLocked = locked;
    }

    public void MarkSoldOut()
    {
        IsSoldOut = true;
        IsLocked = false;
    }

    public void SetPriceModifier(string sourceId, float multiplier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("商品价格修饰来源不能为空。", nameof(sourceId));
        }

        priceModifiers[sourceId] = Math.Max(0f, multiplier);
    }

    public bool TryGetPriceModifier(string sourceId, out float multiplier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            multiplier = 1f;
            return false;
        }

        return priceModifiers.TryGetValue(sourceId, out multiplier);
    }

    public bool RemovePriceModifier(string sourceId)
    {
        return !string.IsNullOrWhiteSpace(sourceId) && priceModifiers.Remove(sourceId);
    }

    public void ClearPriceModifiers()
    {
        priceModifiers.Clear();
    }

    public float GetPriceModifierMultiplier()
    {
        float multiplier = 1f;
        foreach (KeyValuePair<string, float> pair in priceModifiers)
        {
            multiplier *= pair.Value;
        }

        return multiplier;
    }

    public ShopOfferSnapshot CreateSnapshot()
    {
        return new ShopOfferSnapshot(
            OfferId,
            SlotIndex,
            Product.Key,
            Product.DisplayItem,
            Product.DisplayName,
            Product.Tier,
            WasLockedLastVisit,
            IsLocked,
            IsSoldOut);
    }

    public ShopOfferViewData CreateViewData(int price, int originalPrice)
    {
        ItemDataSO displayItem = Product.DisplayItem;
        return new ShopOfferViewData(
            OfferId,
            SlotIndex,
            Product.Key,
            displayItem,
            Product.DisplayName,
            Product.TypeText,
            displayItem != null ? displayItem.ItemIcon : null,
            Product.Tier,
            price,
            originalPrice,
            WasLockedLastVisit,
            IsLocked,
            IsSoldOut,
            Product.BuildInfoDocument());
    }
}
