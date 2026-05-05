using System;

public sealed class ShopPageController
{
    private readonly ShopUIPage view;
    private readonly ShopPageContext context;
    private readonly ShopPageState state = new ShopPageState();
    private readonly CurrencyWallet currencyWallet;
    private bool entered;

    public ShopPageController(ShopUIPage view, ShopPageContext context)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        currencyWallet = context.CurrencyWallet;
    }

    public void Enter()
    {
        if (entered)
        {
            return;
        }

        view.RerollRequested += OnRerollRequested;
        view.ContinueRequested += OnContinueRequested;
        view.PropertiesToggleRequested += OnPropertiesToggleRequested;
        view.InventoryToggleRequested += OnInventoryToggleRequested;
        view.ItemBuyRequested += OnItemBuyRequested;
        view.ItemLockToggleRequested += OnItemLockToggleRequested;

        context.ShopManager.ItemsChanged += OnSnapshotChanged;
        context.ShopManager.PurchaseSucceeded += OnPurchaseSucceeded;
        context.ShopManager.PurchaseFailed += OnPurchaseFailed;
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        view.PrepareForOpen(context);
        view.SetPropertiesSidebarVisible(state.IsPropertiesSidebarVisible);
        view.SetInventorySidebarVisible(state.IsInventorySidebarVisible);
        if (context.CurrencyWallet != null)
        {
            view.UpdateCurrencyAmount(context.CurrencyWallet.CurrentAmount);
        }

        context.ShopManager.RequestSnapshot();
        entered = true;
    }

    public void Exit()
    {
        if (!entered)
        {
            return;
        }

        view.RerollRequested -= OnRerollRequested;
        view.ContinueRequested -= OnContinueRequested;
        view.PropertiesToggleRequested -= OnPropertiesToggleRequested;
        view.InventoryToggleRequested -= OnInventoryToggleRequested;
        view.ItemBuyRequested -= OnItemBuyRequested;
        view.ItemLockToggleRequested -= OnItemLockToggleRequested;

        context.ShopManager.ItemsChanged -= OnSnapshotChanged;
        context.ShopManager.PurchaseSucceeded -= OnPurchaseSucceeded;
        context.ShopManager.PurchaseFailed -= OnPurchaseFailed;
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        view.ResetAfterClose();
        entered = false;
    }

    private void OnSnapshotChanged(ShopSnapshot snapshot)
    {
        view.UpdateRerollState(snapshot.RerollCost, snapshot.CanReroll);
        view.RenderShopItems(snapshot.Items, snapshot.Reason);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccess result)
    {
        view.ShowPurchaseSuccess(result);
    }

    private void OnPurchaseFailed(ShopPurchaseFailure failure)
    {
        view.ShowPurchaseFailure(failure.Message);
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        if (currencyWallet != null && eventData.Wallet != currencyWallet)
        {
            return;
        }

        view.UpdateCurrencyAmount(eventData.CurrentAmount);
    }

    private void OnRerollRequested()
    {
        context.ShopManager.RequestReroll();
    }

    private void OnContinueRequested()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    private void OnPropertiesToggleRequested()
    {
        state.TogglePropertiesSidebar();
        view.SetPropertiesSidebarVisible(state.IsPropertiesSidebarVisible);
    }

    private void OnInventoryToggleRequested()
    {
        state.ToggleInventorySidebar();
        view.SetInventorySidebarVisible(state.IsInventorySidebarVisible);
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        context.ShopManager.RequestBuyItem(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        context.ShopManager.RequestToggleLock(itemIndex);
    }
}
