using System;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 每次进入商店后的首笔成功购买半价。
/// Feature 仅订阅商店事件，不参与商店流程装配。
/// </summary>
[Serializable]
public sealed class FirstPurchaseHalfPriceFeature : FeatureBase
{
    [SerializeField, Range(1, 100)] private int discountPercent = 50;

    private ShopManager shopManager;
    private string runtimeSourceId;
    private bool couponAvailable;

    public override string Title => "首购折扣";
    public override string Description => $"每次进入商店后的首笔成功购买享受 {Mathf.Clamp(discountPercent, 1, 100)}% 折扣。";

    public override void OnInstall()
    {
        runtimeSourceId = ResolveRuntimeSourceId();
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(FirstPurchaseHalfPriceFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.VisitOpened += OnVisitOpened;
        shopManager.PurchaseCompleted += OnPurchaseCompleted;

        if (shopManager.IsVisitOpen && IsEventForOwner(shopManager.CurrentPlayer))
        {
            EnableCoupon();
        }
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.VisitOpened -= OnVisitOpened;
            shopManager.PurchaseCompleted -= OnPurchaseCompleted;
            shopManager.Board.RemovePriceModifier(runtimeSourceId);
            shopManager = null;
        }

        couponAvailable = false;
        runtimeSourceId = null;
    }

    private void OnVisitOpened()
    {
        if (IsEventForOwner(shopManager.CurrentPlayer))
        {
            EnableCoupon();
        }
    }

    private void OnPurchaseCompleted(ShopPurchaseSuccess _)
    {
        if (!couponAvailable || !IsEventForOwner(shopManager.CurrentPlayer))
        {
            return;
        }

        couponAvailable = false;
        shopManager.Board.RemovePriceModifier(runtimeSourceId);
    }

    private void EnableCoupon()
    {
        couponAvailable = true;
        float multiplier = 1f - Mathf.Clamp(discountPercent, 1, 100) / 100f;
        shopManager.Board.SetPriceModifier(runtimeSourceId, multiplier);
    }

    private bool IsEventForOwner(Player eventPlayer)
    {
        return Context?.OwnerEntity is Player player && eventPlayer == player;
    }

    private string ResolveRuntimeSourceId()
    {
        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(FirstPurchaseHalfPriceFeature)}:{GetHashCode()}"
            : $"{SourceId}:{nameof(FirstPurchaseHalfPriceFeature)}:{GetHashCode()}";
    }
}
