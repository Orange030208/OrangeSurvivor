using System;

public sealed class ManagerInventoryUiFacade : IInventoryUiFacade
{
    private readonly InventoryOperateManager manager;
    private bool active;

    public ManagerInventoryUiFacade(InventoryOperateManager manager)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public event Action<InventoryUIItemSnapshot[]> SnapshotChanged;
    public event Action<InventoryItemOperateResource> OperatePanelOpened;
    public event Action<string> OperatePanelShouldClose;

    public void Activate()
    {
        if (active)
        {
            return;
        }

        manager.SnapshotChanged += OnSnapshotChanged;
        manager.OperatePanelOpened += OnOperatePanelOpened;
        manager.OperatePanelShouldClose += OnOperatePanelShouldClose;
        active = true;
    }

    public void Deactivate()
    {
        if (!active)
        {
            return;
        }

        manager.SnapshotChanged -= OnSnapshotChanged;
        manager.OperatePanelOpened -= OnOperatePanelOpened;
        manager.OperatePanelShouldClose -= OnOperatePanelShouldClose;
        active = false;
    }

    public void RequestSnapshot()
    {
        manager.RequestSnapshot();
    }

    public void RequestOpenItemPanel(string entryId)
    {
        manager.RequestOpenItemPanel(entryId);
    }

    public void RequestSellItem(string entryId)
    {
        manager.RequestSellItem(entryId);
    }

    public void RequestMergeItem(string entryId)
    {
        manager.RequestMergeItem(entryId);
    }

    public void Dispose()
    {
        Deactivate();
    }

    private void OnSnapshotChanged(InventoryUIItemSnapshot[] items)
    {
        SnapshotChanged?.Invoke(items);
    }

    private void OnOperatePanelOpened(InventoryItemOperateResource resource)
    {
        OperatePanelOpened?.Invoke(resource);
    }

    private void OnOperatePanelShouldClose(string entryId)
    {
        OperatePanelShouldClose?.Invoke(entryId);
    }
}
