using System;
using UnityEngine;

public sealed class ResolvingInventoryUiFacade : IInventoryUiFacade
{
    private IInventoryUiFacade directFacade;
    private bool active;
    private bool directFacadeBound;
    private bool missingManagerWarningLogged;

    public event Action<InventoryUIItemSnapshot[]> SnapshotChanged;
    public event Action<InventoryItemOperateResource> OperatePanelOpened;
    public event Action<string> OperatePanelShouldClose;

    public void Activate()
    {
        if (active)
        {
            return;
        }

        active = true;
        EnsureDirectFacadeActive();
    }

    public void Deactivate()
    {
        if (!active)
        {
            return;
        }

        if (directFacadeBound)
        {
            directFacade.SnapshotChanged -= OnDirectSnapshotChanged;
            directFacade.OperatePanelOpened -= OnDirectOperatePanelOpened;
            directFacade.OperatePanelShouldClose -= OnDirectOperatePanelShouldClose;
            directFacade.Deactivate();
            directFacadeBound = false;
        }

        active = false;
    }

    public void RequestSnapshot()
    {
        if (!TryGetDirectFacade(out IInventoryUiFacade facade))
        {
            return;
        }

        facade.RequestSnapshot();
    }

    public void RequestOpenItemPanel(string entryId)
    {
        if (!TryGetDirectFacade(out IInventoryUiFacade facade))
        {
            return;
        }

        facade.RequestOpenItemPanel(entryId);
    }

    public void RequestSellItem(string entryId)
    {
        if (!TryGetDirectFacade(out IInventoryUiFacade facade))
        {
            return;
        }

        facade.RequestSellItem(entryId);
    }

    public void RequestMergeItem(string entryId)
    {
        if (!TryGetDirectFacade(out IInventoryUiFacade facade))
        {
            return;
        }

        facade.RequestMergeItem(entryId);
    }

    public void Dispose()
    {
        Deactivate();
        directFacade?.Dispose();
        directFacade = null;
    }

    private void OnDirectSnapshotChanged(InventoryUIItemSnapshot[] items)
    {
        SnapshotChanged?.Invoke(items);
    }

    private void OnDirectOperatePanelOpened(InventoryItemOperateResource resource)
    {
        OperatePanelOpened?.Invoke(resource);
    }

    private void OnDirectOperatePanelShouldClose(string entryId)
    {
        OperatePanelShouldClose?.Invoke(entryId);
    }

    private bool TryGetDirectFacade(out IInventoryUiFacade facade)
    {
        EnsureDirectFacadeActive();
        facade = directFacade;

        if (facade != null)
        {
            return true;
        }

        if (!missingManagerWarningLogged)
        {
            Debug.LogWarning($"{nameof(ResolvingInventoryUiFacade)} failed to resolve {nameof(InventoryOperateManager)}. Inventory UI commands will be ignored.");
            missingManagerWarningLogged = true;
        }

        return false;
    }

    private void EnsureDirectFacadeActive()
    {
        if (!active || directFacadeBound)
        {
            return;
        }

        if (directFacade == null)
        {
            InventoryOperateManager inventoryOperateManager = UnityEngine.Object.FindFirstObjectByType<InventoryOperateManager>();
            if (inventoryOperateManager == null)
            {
                return;
            }

            directFacade = new ManagerInventoryUiFacade(inventoryOperateManager);
        }

        directFacade.SnapshotChanged += OnDirectSnapshotChanged;
        directFacade.OperatePanelOpened += OnDirectOperatePanelOpened;
        directFacade.OperatePanelShouldClose += OnDirectOperatePanelShouldClose;
        directFacade.Activate();
        directFacadeBound = true;
    }
}
