using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class InventoryUI : ViewPartBase
{
    private const string POPUP_GROUP_ID = "inventory.operate";

    [Header("容器与预制体")]
    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private Transform itemContainersParent;

    private readonly List<InventoryItem> spawnedItems = new();

    private InventoryOperateManager inventoryOperateManagerSession;
    private InventoryOperateManager configuredInventoryOperateManager;
    private UIManager uiManager;
    private bool inventorySessionStarted;
    private InventoryItemViewData[] currentItems = Array.Empty<InventoryItemViewData>();
    private string currentSelectedEntryId;
    private string currentOperateEntryId;
    private int popupVersion;
    private ViewHandle currentPopupHandle;
    private bool cancelInputBound;

    private void Awake()
    {
        ValidateConfiguration();
        itemContainersParent.Clear();
    }

    private void OnEnable()
    {
        StartInventorySession();
        BindCancelInput();
    }

    private void OnDisable()
    {
        UnbindCancelInput();
        StopInventorySession();
    }

    private void BindCancelInput()
    {
        if (cancelInputBound)
        {
            return;
        }

        GameInput input = GameInput.Instance;
        if (input == null)
        {
            return;
        }

        input.UiCancelPerformed += OnCancelInputPerformed;
        cancelInputBound = true;
    }

    private void UnbindCancelInput()
    {
        if (!cancelInputBound)
        {
            return;
        }

        GameInput input = GameInput.Instance;
        if (input != null)
        {
            input.UiCancelPerformed -= OnCancelInputPerformed;
        }

        cancelInputBound = false;
    }

    private void OnCancelInputPerformed()
    {
        if (!HasOpenPopup)
        {
            return;
        }

        AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
        ClosePopup();
    }

    public void WarmUp()
    {
        if (itemContainersParent != null && spawnedItems.Count == 0)
        {
            itemContainersParent.Clear();
        }
    }

    public void ConfigureSession(InventoryOperateManager manager, UIManager ownerUIManager)
    {
        bool sameManager = configuredInventoryOperateManager == manager;
        uiManager = ownerUIManager;

        if (sameManager)
        {
            if (!inventorySessionStarted && isActiveAndEnabled && manager != null)
            {
                StartInventorySession();
            }

            return;
        }

        bool shouldRebind = inventorySessionStarted && isActiveAndEnabled;
        if (shouldRebind)
        {
            StopInventorySession();
        }

        configuredInventoryOperateManager = manager;

        if (isActiveAndEnabled && manager != null)
        {
            StartInventorySession();
        }
    }

    public void ReleaseSession()
    {
        if (inventorySessionStarted && ReferenceEquals(inventoryOperateManagerSession, configuredInventoryOperateManager))
        {
            StopInventorySession();
        }

        configuredInventoryOperateManager = null;
        uiManager = null;
    }

    private void PrepareForOpen()
    {
        ClosePopupState();
        CloseCurrentPopupHandleAsync(CloseReason.Normal).Forget();
    }

    private void ResetAfterClose()
    {
        currentItems = Array.Empty<InventoryItemViewData>();
        currentSelectedEntryId = null;
        ClosePopupState();
        ClearItems();
        CloseCurrentPopupHandleAsync(CloseReason.Normal).Forget();
    }

    private InventoryOperateManager ResolveInventoryOperateManagerSession()
    {
        if (configuredInventoryOperateManager != null)
        {
            return configuredInventoryOperateManager;
        }

        throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' requires an externally configured {nameof(InventoryOperateManager)} session.");
    }

    private void StartInventorySession()
    {
        if (inventorySessionStarted)
        {
            return;
        }

        if (configuredInventoryOperateManager == null)
        {
            return;
        }

        inventoryOperateManagerSession = ResolveInventoryOperateManagerSession();
        inventoryOperateManagerSession.ItemsChanged += OnItemsChanged;
        inventoryOperateManagerSession.OperatePanelOpened += OnOperatePanelOpened;
        inventoryOperateManagerSession.OperatePanelShouldClose += OnOperatePanelShouldClose;

        PrepareForOpen();
        inventoryOperateManagerSession.RefreshItems();
        inventorySessionStarted = true;
    }

    private void StopInventorySession()
    {
        if (!inventorySessionStarted)
        {
            return;
        }

        inventoryOperateManagerSession.ItemsChanged -= OnItemsChanged;
        inventoryOperateManagerSession.OperatePanelOpened -= OnOperatePanelOpened;
        inventoryOperateManagerSession.OperatePanelShouldClose -= OnOperatePanelShouldClose;

        ResetAfterClose();
        inventorySessionStarted = false;
        inventoryOperateManagerSession = null;
    }

    private void ValidateConfiguration()
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing {nameof(InventoryItem)} prefab.");
        }

        if (itemContainersParent == null)
        {
            throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' is missing item containers parent.");
        }
    }

    private void OnItemSelected(string entryId)
    {
        if (string.IsNullOrEmpty(entryId) || inventoryOperateManagerSession == null)
        {
            return;
        }

        currentSelectedEntryId = entryId;
        inventoryOperateManagerSession.RequestOpenItemPanel(entryId);
    }

    private void OnSellRequested(string entryId)
    {
        if (!IsShowingItem(entryId) || inventoryOperateManagerSession == null)
        {
            return;
        }

        inventoryOperateManagerSession.RequestSellItem(entryId);
    }

    private void OnMergeRequested(string entryId)
    {
        if (!IsShowingItem(entryId) || inventoryOperateManagerSession == null)
        {
            return;
        }

        inventoryOperateManagerSession.RequestMergeItem(entryId);
    }

    private void OnItemsChanged(InventoryItemViewData[] items)
    {
        SyncItems(items, out bool shouldClosePopup, out string popupEntryIdToRestore);
        RenderItems(items);

        if (shouldClosePopup)
        {
            CloseCurrentPopupHandleAsync(CloseReason.Normal).Forget();
            return;
        }

        if (!string.IsNullOrEmpty(popupEntryIdToRestore))
        {
            inventoryOperateManagerSession.RequestOpenItemPanel(popupEntryIdToRestore);
        }
    }

    private void OnOperatePanelOpened(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null || string.IsNullOrEmpty(resource.entryId))
        {
            return;
        }

        if (!HasItem(resource.entryId))
        {
            return;
        }

        OpenPopupState(resource.entryId);
        ShowOperatePopup(resource);
    }

    private void OnOperatePanelShouldClose(string entryId)
    {
        if (!IsShowingItem(entryId))
        {
            return;
        }

        ClosePopup();
    }

    private void RenderItems(InventoryItemViewData[] items)
    {
        ClearItems();
        if (items == null || items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            SpawnItem(items[i]);
        }
    }

    private void SpawnItem(InventoryItemViewData itemViewData)
    {
        if (itemViewData.ItemData == null || string.IsNullOrEmpty(itemViewData.EntryId))
        {
            return;
        }

        InventoryItem item = Instantiate(itemPrefab, itemContainersParent);
        item.Configure(itemViewData.EntryId, itemViewData.ItemData, itemViewData.ColorDependencyNumber);
        item.Clicked += OnItemSelected;
        spawnedItems.Add(item);
    }

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItem item = spawnedItems[i];
            if (item == null)
            {
                continue;
            }

            item.Clicked -= OnItemSelected;
            item.Dispose();
            Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void ShowOperatePopup(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            return;
        }

        int version = ++popupVersion;
        currentOperateEntryId = resource.entryId;
        ShowOperatePopupAsync(resource, version).Forget();
    }

    private async UniTaskVoid ShowOperatePopupAsync(InventoryItemOperateResource resource, int version)
    {
        try
        {
            await CloseCurrentPopupHandleAsync(CloseReason.Replace, invalidateRequest: false, clearPopupState: false);
            if (version != popupVersion)
            {
                return;
            }

            PopupOptions options = new PopupOptions(
                closeOnOutsideClick: true,
                groupId: POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: true,
                preferredAnchor: FloatingViewAnchor.Center);

            UIManager manager = ResolveUIManager();
            if (resource.itemData.ItemType == ItemType.Weapon)
            {
                ViewHandle<WeaponOperatePopup> handle = await manager.ShowPopupAsync<WeaponOperatePopup>(resource, options);
                if (version != popupVersion)
                {
                    await handle.CloseAsync(CloseReason.Cancel);
                    return;
                }

                currentPopupHandle = handle.AsUntyped();
                handle.View.SellRequested += OnSellRequested;
                handle.View.MergeRequested += OnMergeRequested;
                ObservePopupClosedAsync(currentPopupHandle, version, resource.entryId).Forget();
                return;
            }

            ViewHandle<AccessoryInfoPopup> accessoryHandle = await manager.ShowPopupAsync<AccessoryInfoPopup>(resource, options);
            if (version != popupVersion)
            {
                await accessoryHandle.CloseAsync(CloseReason.Cancel);
                return;
            }

            currentPopupHandle = accessoryHandle.AsUntyped();
            ObservePopupClosedAsync(currentPopupHandle, version, resource.entryId).Forget();
        }
        catch (Exception exception)
        {
            if (version == popupVersion)
            {
                currentOperateEntryId = null;
            }

            Debug.LogException(exception);
        }
    }

    private async UniTaskVoid ObservePopupClosedAsync(ViewHandle handle, int version, string entryId)
    {
        try
        {
            await handle.ClosedTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }

        if (version != popupVersion || !string.Equals(currentOperateEntryId, entryId, StringComparison.Ordinal))
        {
            return;
        }

        currentOperateEntryId = null;
        currentPopupHandle = default;
        ClosePopupState();
    }

    private async UniTask CloseCurrentPopupHandleAsync(
        CloseReason reason,
        bool invalidateRequest = true,
        bool clearPopupState = true)
    {
        if (invalidateRequest)
        {
            popupVersion++;
        }

        if (clearPopupState)
        {
            currentOperateEntryId = null;
        }

        ViewHandle handle = currentPopupHandle;
        currentPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
    }

    private UIManager ResolveUIManager()
    {
        if (uiManager != null)
        {
            return uiManager;
        }

        throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' requires an explicit {nameof(UIManager)} before inventory operate popups can be opened.");
    }

    private void ClosePopup()
    {
        ClosePopupState();
        CloseCurrentPopupHandleAsync(CloseReason.Normal).Forget();
    }

    private void OpenPopupState(string entryId)
    {
        currentSelectedEntryId = entryId;
        currentOperateEntryId = entryId;
    }

    private void ClosePopupState()
    {
        currentSelectedEntryId = null;
        currentOperateEntryId = null;
    }

    private bool HasOpenPopup => !string.IsNullOrEmpty(currentOperateEntryId);

    private bool IsShowingItem(string entryId)
    {
        return currentOperateEntryId == entryId;
    }

    private bool HasItem(string entryId)
    {
        return ContainsEntry(currentItems, entryId);
    }

    private void SyncItems(
        InventoryItemViewData[] items,
        out bool shouldClosePopup,
        out string popupEntryIdToRestore)
    {
        bool hadOpenPopup = HasOpenPopup;
        string previousPopupEntryId = currentOperateEntryId;

        currentItems = items ?? Array.Empty<InventoryItemViewData>();

        if (!ContainsEntry(currentItems, currentSelectedEntryId))
        {
            currentSelectedEntryId = null;
        }

        bool popupStillExists = ContainsEntry(currentItems, previousPopupEntryId);
        if (!popupStillExists)
        {
            currentOperateEntryId = null;
            shouldClosePopup = hadOpenPopup;
            popupEntryIdToRestore = null;
            return;
        }

        currentOperateEntryId = previousPopupEntryId;
        shouldClosePopup = false;
        popupEntryIdToRestore = previousPopupEntryId;
    }

    private static bool ContainsEntry(InventoryItemViewData[] items, string entryId)
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
