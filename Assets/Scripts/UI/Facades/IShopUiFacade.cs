using System;

public interface IShopUiFacade : IDisposable
{
    event Action<ShopSnapshot> SnapshotChanged;
    event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    event Action<ShopPurchaseFailure> PurchaseFailed;
    event Action<int> CurrencyChanged;

    void Activate();
    void Deactivate();
    void RequestSnapshot();
    void RequestReroll();
    void RequestContinue();
    void RequestBuyItem(int itemIndex);
    void RequestToggleLock(int itemIndex);
}
