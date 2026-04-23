using System;

public sealed class InventoryRegionController : IDisposable
{
    private readonly IInventoryRegionView view;
    private readonly IInventoryUiFacade facade;
    private readonly InventoryRegionState state = new();
    private bool entered;

    public InventoryRegionController(IInventoryRegionView view, IInventoryUiFacade facade)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    public void Enter()
    {
        if (entered)
        {
            return;
        }

        view.ItemSelected += OnItemSelected;
        view.CloseRequested += OnCloseRequested;
        view.SellRequested += OnSellRequested;
        view.MergeRequested += OnMergeRequested;

        facade.SnapshotChanged += OnSnapshotChanged;
        facade.OperatePanelOpened += OnOperatePanelOpened;
        facade.OperatePanelShouldClose += OnOperatePanelShouldClose;
        facade.Activate();

        view.PrepareForOpen();
        facade.RequestSnapshot();
        entered = true;
    }

    public void Exit()
    {
        if (!entered)
        {
            return;
        }

        view.ItemSelected -= OnItemSelected;
        view.CloseRequested -= OnCloseRequested;
        view.SellRequested -= OnSellRequested;
        view.MergeRequested -= OnMergeRequested;

        facade.SnapshotChanged -= OnSnapshotChanged;
        facade.OperatePanelOpened -= OnOperatePanelOpened;
        facade.OperatePanelShouldClose -= OnOperatePanelShouldClose;
        facade.Deactivate();

        state.ClosePopup();
        view.ResetAfterClose();
        entered = false;
    }

    public void Dispose()
    {
        Exit();
    }

    private void OnSnapshotChanged(InventoryUIItemSnapshot[] items)
    {
        state.SyncSnapshot(items, out bool shouldClosePopup, out string popupEntryIdToRestore);
        view.RenderItems(items);

        if (shouldClosePopup)
        {
            view.CloseOperatePopup();
            return;
        }

        if (!string.IsNullOrEmpty(popupEntryIdToRestore))
        {
            facade.RequestOpenItemPanel(popupEntryIdToRestore);
        }
    }

    private void OnOperatePanelOpened(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null || string.IsNullOrEmpty(resource.entryId))
        {
            return;
        }

        if (!state.HasItem(resource.entryId))
        {
            return;
        }

        state.OpenPopup(resource.entryId);
        view.ShowOperatePopup(resource);
    }

    private void OnOperatePanelShouldClose(string entryId)
    {
        if (!state.IsShowingItem(entryId))
        {
            return;
        }

        ClosePopup();
    }

    private void OnItemSelected(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            return;
        }

        state.SelectItem(entryId);
        facade.RequestOpenItemPanel(entryId);
    }

    private void OnCloseRequested()
    {
        if (!state.HasOpenPopup)
        {
            return;
        }

        ClosePopup();
    }

    private void OnSellRequested(string entryId)
    {
        if (!state.IsShowingItem(entryId))
        {
            return;
        }

        facade.RequestSellItem(entryId);
    }

    private void OnMergeRequested(string entryId)
    {
        if (!state.IsShowingItem(entryId))
        {
            return;
        }

        facade.RequestMergeItem(entryId);
    }

    private void ClosePopup()
    {
        state.ClosePopup();
        view.CloseOperatePopup();
    }
}
