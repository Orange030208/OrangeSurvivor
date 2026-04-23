using System;

public interface IInventoryUiFacade : IDisposable
{
    event Action<InventoryUIItemSnapshot[]> SnapshotChanged;
    event Action<InventoryItemOperateResource> OperatePanelOpened;
    event Action<string> OperatePanelShouldClose;

    void Activate();
    void Deactivate();
    void RequestSnapshot();
    void RequestOpenItemPanel(string entryId);
    void RequestSellItem(string entryId);
    void RequestMergeItem(string entryId);
}
