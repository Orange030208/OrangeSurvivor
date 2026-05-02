using System;

public sealed class ShopPageController : IPageController
{
    private readonly IShopPageView view;
    private readonly ShopPageContext context;
    private readonly ShopPageState state = new ShopPageState();
    private bool entered;

    public ShopPageController(IShopPageView view, ShopPageContext context)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
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

        context.ShopFacade.SnapshotChanged += OnSnapshotChanged;
        context.ShopFacade.PurchaseSucceeded += OnPurchaseSucceeded;
        context.ShopFacade.PurchaseFailed += OnPurchaseFailed;
        context.ShopFacade.CurrencyChanged += OnCurrencyChanged;
        context.ShopFacade.Activate();

        view.PrepareForOpen(context);
        view.SetPropertiesSidebarVisible(state.IsPropertiesSidebarVisible);
        view.SetInventorySidebarVisible(state.IsInventorySidebarVisible);
        if (context.CurrencyWallet != null)
        {
            view.UpdateCurrencyAmount(context.CurrencyWallet.CurrentAmount);
        }

        context.ShopFacade.RequestSnapshot();
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

        context.ShopFacade.SnapshotChanged -= OnSnapshotChanged;
        context.ShopFacade.PurchaseSucceeded -= OnPurchaseSucceeded;
        context.ShopFacade.PurchaseFailed -= OnPurchaseFailed;
        context.ShopFacade.CurrencyChanged -= OnCurrencyChanged;
        context.ShopFacade.Deactivate();

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

    private void OnCurrencyChanged(int currentAmount)
    {
        view.UpdateCurrencyAmount(currentAmount);
    }

    private void OnRerollRequested()
    {
        context.ShopFacade.RequestReroll();
    }

    private void OnContinueRequested()
    {
        context.ShopFacade.RequestContinue();
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
        context.ShopFacade.RequestBuyItem(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        context.ShopFacade.RequestToggleLock(itemIndex);
    }
}
