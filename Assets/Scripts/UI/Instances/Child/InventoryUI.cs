using System;
using Orange.UIFramework;
using UnityEngine;

public class InventoryUI : ViewPartBase
{
    [Header("容器与预制体")]
    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private Transform itemContainersParent;

    [Header("运行时 Manager")]
    [SerializeField] private InventoryOperateManager inventoryOperateManager;

    private InventoryOperateManager inventoryOperateManagerSession;
    private InventoryOperateManager configuredInventoryOperateManager;
    private bool inventorySessionStarted;
    private InventoryUIItemSnapshot[] currentItems = Array.Empty<InventoryUIItemSnapshot>();
    private string currentSelectedEntryId;
    private string currentOperateEntryId;
    private InventoryListView listView;
    private InventoryOperatePopupHost popupHost;

    private void Awake()
    {
        ValidateConfiguration();
        listView = new InventoryListView(name, itemPrefab, itemContainersParent);
        popupHost = new InventoryOperatePopupHost(name);
        listView.ItemClicked += OnItemSelected;
        popupHost.CloseRequested += OnCloseRequested;
        popupHost.SellRequested += OnSellRequested;
        popupHost.MergeRequested += OnMergeRequested;
    }

    private void OnEnable()
    {
        StartInventorySession();
    }

    private void Update()
    {
        if (!popupHost.HasOpenPopup)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
            OnCloseRequested();
        }
    }

    private void OnDisable()
    {
        StopInventorySession();
    }

    public void ConfigureInventoryOperateManager(InventoryOperateManager manager)
    {
        if (configuredInventoryOperateManager == manager)
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

    public void ConfigureUIManager(UIManager manager)
    {
        popupHost.ConfigureUIManager(manager);
    }

    public void ReleaseConfiguredInventoryOperateManager()
    {
        if (inventorySessionStarted && ReferenceEquals(inventoryOperateManagerSession, configuredInventoryOperateManager))
        {
            StopInventorySession();
        }

        configuredInventoryOperateManager = null;
    }

    private void PrepareForOpen()
    {
        ClosePopupState();
        popupHost.CloseCurrent();
    }

    private void ResetAfterClose()
    {
        currentItems = Array.Empty<InventoryUIItemSnapshot>();
        currentSelectedEntryId = null;
        ClosePopupState();
        listView.Clear();
        popupHost.CloseCurrent();
    }

    private void RenderItems(InventoryUIItemSnapshot[] items)
    {
        listView.Render(items);
    }

    private void ShowOperatePopup(InventoryItemOperateResource resource)
    {
        popupHost.Show(resource);
    }

    private void CloseOperatePopup()
    {
        popupHost.CloseCurrent();
    }

    private InventoryOperateManager ResolveInventoryOperateManagerSession()
    {
        if (configuredInventoryOperateManager != null)
        {
            return configuredInventoryOperateManager;
        }

        InventoryOperateManager resolvedManager = ResolveInventoryOperateManager();
        if (resolvedManager != null)
        {
            return resolvedManager;
        }

        throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' requires either an externally configured or locally serialized {nameof(InventoryOperateManager)} reference.");
    }

    private void StartInventorySession()
    {
        if (inventorySessionStarted)
        {
            return;
        }

        if (configuredInventoryOperateManager == null && ResolveInventoryOperateManager() == null)
        {
            return;
        }

        inventoryOperateManagerSession = ResolveInventoryOperateManagerSession();
        inventoryOperateManagerSession.SnapshotChanged += OnSnapshotChanged;
        inventoryOperateManagerSession.OperatePanelOpened += OnOperatePanelOpened;
        inventoryOperateManagerSession.OperatePanelShouldClose += OnOperatePanelShouldClose;

        PrepareForOpen();
        inventoryOperateManagerSession.RequestSnapshot();
        inventorySessionStarted = true;
    }

    private void StopInventorySession()
    {
        if (!inventorySessionStarted)
        {
            return;
        }

        inventoryOperateManagerSession.SnapshotChanged -= OnSnapshotChanged;
        inventoryOperateManagerSession.OperatePanelOpened -= OnOperatePanelOpened;
        inventoryOperateManagerSession.OperatePanelShouldClose -= OnOperatePanelShouldClose;

        ResetAfterClose();
        inventorySessionStarted = false;
        inventoryOperateManagerSession = null;
    }

    private InventoryOperateManager ResolveInventoryOperateManager()
    {
        return inventoryOperateManager;
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

    private void OnCloseRequested()
    {
        if (!HasOpenPopup)
        {
            return;
        }

        ClosePopup();
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

    private void OnSnapshotChanged(InventoryUIItemSnapshot[] items)
    {
        SyncSnapshot(items, out bool shouldClosePopup, out string popupEntryIdToRestore);
        RenderItems(items);

        if (shouldClosePopup)
        {
            CloseOperatePopup();
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

    private void ClosePopup()
    {
        ClosePopupState();
        CloseOperatePopup();
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

    private void SyncSnapshot(
        InventoryUIItemSnapshot[] items,
        out bool shouldClosePopup,
        out string popupEntryIdToRestore)
    {
        bool hadOpenPopup = HasOpenPopup;
        string previousPopupEntryId = currentOperateEntryId;

        currentItems = items ?? Array.Empty<InventoryUIItemSnapshot>();

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
