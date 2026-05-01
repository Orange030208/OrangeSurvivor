using System;

public sealed class ManagerShopUiFacade : IShopUiFacade
{
    private readonly ShopManager manager;
    private readonly CurrencyWallet currencyWallet;
    private bool active;

    public ManagerShopUiFacade(ShopManager manager, CurrencyWallet currencyWallet)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.currencyWallet = currencyWallet;
    }

    public event Action<ShopSnapshot> SnapshotChanged;
    public event Action<ShopPurchaseSuccess> PurchaseSucceeded;
    public event Action<ShopPurchaseFailure> PurchaseFailed;
    public event Action<int> CurrencyChanged;

    public void Activate()
    {
        if (active)
        {
            return;
        }

        manager.ItemsChanged += OnItemsChanged;
        manager.PurchaseSucceeded += OnPurchaseSucceeded;
        manager.PurchaseFailed += OnPurchaseFailed;
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        active = true;
    }

    public void Deactivate()
    {
        if (!active)
        {
            return;
        }

        manager.ItemsChanged -= OnItemsChanged;
        manager.PurchaseSucceeded -= OnPurchaseSucceeded;
        manager.PurchaseFailed -= OnPurchaseFailed;
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        active = false;
    }

    public void RequestSnapshot()
    {
        manager.RequestSnapshot();
    }

    public void RequestReroll()
    {
        manager.RequestReroll();
    }

    public void RequestContinue()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    public void RequestBuyItem(int itemIndex)
    {
        manager.RequestBuyItem(itemIndex);
    }

    public void RequestToggleLock(int itemIndex)
    {
        manager.RequestToggleLock(itemIndex);
    }

    public void Dispose()
    {
        Deactivate();
    }

    private void OnItemsChanged(ShopSnapshot snapshot)
    {
        SnapshotChanged?.Invoke(snapshot);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccess result)
    {
        PurchaseSucceeded?.Invoke(result);
    }

    private void OnPurchaseFailed(ShopPurchaseFailure failure)
    {
        PurchaseFailed?.Invoke(failure);
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
