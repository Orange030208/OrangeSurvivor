using System;

public interface IShopUiFacade : IDisposable
{
    event Action<ShopItemsChangedEvent> SnapshotChanged;
    event Action<ShopPurchaseSuccessEvent> PurchaseSucceeded;
    event Action<ShopPurchaseFailedEvent> PurchaseFailed;
    event Action<int> CurrencyChanged;

    void Activate();
    void Deactivate();
    void RequestSnapshot();
    void RequestReroll();
    void RequestContinue();
    void RequestBuyItem(int itemIndex);
    void RequestToggleLock(int itemIndex);
}
