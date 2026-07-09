using System;

public interface IShopController
{
    event Action<ShopViewState> ViewStateChanged;
    event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    event Action<ShopPurchaseFailure> PurchaseFailed;

    void RefreshViewState();
    void RequestBuyItem(int itemIndex);
    void RequestReroll();
    void RequestToggleLock(int itemIndex);
}
