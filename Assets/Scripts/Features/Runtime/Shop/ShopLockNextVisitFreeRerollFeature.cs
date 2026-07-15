using System;
using Orange.GameServices;
using UnityEngine;

[Serializable]
public sealed class ShopLockNextVisitFreeRerollFeature : FeatureBase
{
    [SerializeField, Min(1)] private int freeRerollCount = 3;

    private ShopManager shopManager;
    private bool hasLockedItemForNextShop;

    public override string Title => "锁柜免费刷新";
    public override string Description =>
        $"每波商店中，若你锁定过至少 1 件商品，则下一次进入商店时获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        if (!GameServices.TryGet(out shopManager))
        {
            Debug.LogWarning($"[{nameof(ShopLockNextVisitFreeRerollFeature)}] {nameof(ShopManager)} is unavailable.");
            return;
        }

        shopManager.LockChanged += OnLockChanged;
        shopManager.VisitOpened += OnVisitOpened;
    }

    public override void OnUninstall()
    {
        if (shopManager != null)
        {
            shopManager.LockChanged -= OnLockChanged;
            shopManager.VisitOpened -= OnVisitOpened;
            shopManager = null;
        }

        hasLockedItemForNextShop = false;
    }

    private void OnLockChanged(ShopOfferState _, bool isLocked)
    {
        if (IsShopOwner() && isLocked)
        {
            hasLockedItemForNextShop = true;
        }
    }

    private void OnVisitOpened()
    {
        if (!IsShopOwner() || !hasLockedItemForNextShop || freeRerollCount <= 0)
        {
            return;
        }

        hasLockedItemForNextShop = false;
        shopManager.Board.GrantVisitFreeRerolls(freeRerollCount);
    }

    private bool IsShopOwner()
    {
        return Context?.OwnerEntity is Player player && shopManager.CurrentPlayer == player;
    }
}
