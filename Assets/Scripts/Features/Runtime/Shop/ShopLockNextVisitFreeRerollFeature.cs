using System;
using UnityEngine;

[Serializable]
public sealed class ShopLockNextVisitFreeRerollFeature : FeatureBase
{
    [SerializeField, Min(1)] private int freeRerollCount = 3;

    private bool hasLockedItemForNextShop;

    public override string Title => "锁柜免费刷新";
    public override string Description =>
        $"每波商店中，若你锁定过至少 1 件商品，则下一次进入商店时获得 {Mathf.Max(1, freeRerollCount)} 次免费刷新。";

    public override void OnInstall()
    {
        YokiFrame.EventKit.Type.Register<ShopItemLockedEvent>(OnShopItemLocked);
        YokiFrame.EventKit.Type.Register<GameStateChangedEvent>(OnGameStateChanged);
    }

    public override void OnUninstall()
    {
        YokiFrame.EventKit.Type.UnRegister<ShopItemLockedEvent>(OnShopItemLocked);
        YokiFrame.EventKit.Type.UnRegister<GameStateChangedEvent>(OnGameStateChanged);
        hasLockedItemForNextShop = false;
    }

    private void OnShopItemLocked(ShopItemLockedEvent eventData)
    {
        if (Context?.OwnerEntity is not Player player || eventData.Player != player)
        {
            return;
        }

        hasLockedItemForNextShop = true;
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState != GameState.Shop || eventData.OldState == GameState.Shop)
        {
            return;
        }

        GrantPendingFreeRerolls();
    }

    private void GrantPendingFreeRerolls()
    {
        if (!hasLockedItemForNextShop || freeRerollCount <= 0 || Context?.OwnerEntity is not Player player)
        {
            return;
        }

        hasLockedItemForNextShop = false;
        YokiFrame.EventKit.Type.Send(new ShopFreeRerollsGrantedEvent(player, freeRerollCount));
    }
}
