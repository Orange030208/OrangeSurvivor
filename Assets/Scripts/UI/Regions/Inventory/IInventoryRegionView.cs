using System;

public interface IInventoryRegionView
{
    event Action<string> ItemSelected;
    event Action CloseRequested;
    event Action<string> SellRequested;
    event Action<string> MergeRequested;

    void PrepareForOpen();
    void ResetAfterClose();
    void RenderItems(InventoryUIItemSnapshot[] items);
    void ShowOperatePopup(InventoryItemOperateResource resource);
    void CloseOperatePopup();
}
