using System;

public sealed class InventoryRegionState
{
    private InventoryUIItemSnapshot[] currentItems = Array.Empty<InventoryUIItemSnapshot>();

    public string CurrentSelectedEntryId { get; private set; }
    public string CurrentOperateEntryId { get; private set; }

    public bool HasSelection => !string.IsNullOrEmpty(CurrentSelectedEntryId);
    public bool HasOpenPopup => !string.IsNullOrEmpty(CurrentOperateEntryId);

    public void SelectItem(string entryId)
    {
        CurrentSelectedEntryId = entryId;
    }

    public void OpenPopup(string entryId)
    {
        CurrentSelectedEntryId = entryId;
        CurrentOperateEntryId = entryId;
    }

    public void ClosePopup()
    {
        CurrentSelectedEntryId = null;
        CurrentOperateEntryId = null;
    }

    public bool IsShowingItem(string entryId)
    {
        return CurrentOperateEntryId == entryId;
    }

    public bool HasItem(string entryId)
    {
        return ContainsEntry(currentItems, entryId);
    }

    public void SyncSnapshot(InventoryUIItemSnapshot[] items, out bool shouldClosePopup, out string popupEntryIdToRestore)
    {
        bool hadOpenPopup = HasOpenPopup;
        string previousPopupEntryId = CurrentOperateEntryId;

        currentItems = items ?? Array.Empty<InventoryUIItemSnapshot>();

        if (!ContainsEntry(currentItems, CurrentSelectedEntryId))
        {
            CurrentSelectedEntryId = null;
        }

        bool popupStillExists = ContainsEntry(currentItems, previousPopupEntryId);
        if (!popupStillExists)
        {
            CurrentOperateEntryId = null;
            shouldClosePopup = hadOpenPopup;
            popupEntryIdToRestore = null;
            return;
        }

        CurrentOperateEntryId = previousPopupEntryId;
        shouldClosePopup = false;
        popupEntryIdToRestore = previousPopupEntryId;
    }

    private static bool ContainsEntry(InventoryUIItemSnapshot[] items, string entryId)
    {
        if (items == null || items.Length == 0 || string.IsNullOrEmpty(entryId))
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].EntryId == entryId)
            {
                return true;
            }
        }

        return false;
    }
}
