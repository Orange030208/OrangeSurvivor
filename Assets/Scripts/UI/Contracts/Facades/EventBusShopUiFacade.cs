using System;

public sealed class EventBusShopUiFacade : IShopUiFacade
{
    private readonly CurrencyWallet currencyWallet;
    private bool active;

    public EventBusShopUiFacade(CurrencyWallet currencyWallet)
    {
        this.currencyWallet = currencyWallet;
    }

    public event Action<ShopItemsChangedEvent> SnapshotChanged;
    public event Action<ShopPurchaseSuccessEvent> PurchaseSucceeded;
    public event Action<ShopPurchaseFailedEvent> PurchaseFailed;
    public event Action<int> CurrencyChanged;

    public void Activate()
    {
        if (active)
        {
            return;
        }

        GameEventBus.Subscribe<ShopItemsChangedEvent>(OnSnapshotChanged);
        GameEventBus.Subscribe<ShopPurchaseSuccessEvent>(OnPurchaseSucceeded);
        GameEventBus.Subscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        active = true;
    }

    public void Deactivate()
    {
        if (!active)
        {
            return;
        }

        GameEventBus.Unsubscribe<ShopItemsChangedEvent>(OnSnapshotChanged);
        GameEventBus.Unsubscribe<ShopPurchaseSuccessEvent>(OnPurchaseSucceeded);
        GameEventBus.Unsubscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        active = false;
    }

    public void RequestSnapshot()
    {
        GameEventBus.Publish(new RequestShopSnapshotEvent());
    }

    public void RequestReroll()
    {
        GameEventBus.Publish(new ShopRerollRequestedEvent());
    }

    public void RequestContinue()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    public void RequestBuyItem(int itemIndex)
    {
        GameEventBus.Publish(new ShopItemClickedEvent(itemIndex));
    }

    public void RequestToggleLock(int itemIndex)
    {
        GameEventBus.Publish(new OperateShopItemLockEvent(itemIndex));
    }

    public void Dispose()
    {
        Deactivate();
    }

    private void OnSnapshotChanged(ShopItemsChangedEvent eventData)
    {
        SnapshotChanged?.Invoke(eventData);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccessEvent eventData)
    {
        PurchaseSucceeded?.Invoke(eventData);
    }

    private void OnPurchaseFailed(ShopPurchaseFailedEvent eventData)
    {
        PurchaseFailed?.Invoke(eventData);
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        if (currencyWallet != null && eventData.Wallet != currencyWallet)
        {
            return;
        }

        CurrencyChanged?.Invoke(eventData.CurrentAmount);
    }
}
