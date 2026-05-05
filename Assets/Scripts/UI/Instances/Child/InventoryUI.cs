using System;
using Orange.UIFramework;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("容器与预制体")]
    [SerializeField] private InventoryItem itemPrefab;
    [SerializeField] private Transform itemContainersParent;

    [Header("Facade")]
    [SerializeField] private InventoryOperateManager inventoryOperateManager;

    private IInventoryUiFacade inventoryFacade;
    private IInventoryUiFacade configuredFacade;
    private bool disposeConfiguredFacade;
    private bool ownsInventoryFacade;
    private bool facadeSessionStarted;
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
        StartFacadeSession();
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
        StopFacadeSession();
    }

    public void ConfigureFacade(IInventoryUiFacade facade, bool takeOwnership = false)
    {
        if (configuredFacade == facade && disposeConfiguredFacade == takeOwnership)
        {
            if (!facadeSessionStarted && isActiveAndEnabled && facade != null)
            {
                StartFacadeSession();
            }

            return;
        }

        bool shouldRebind = facadeSessionStarted && isActiveAndEnabled;
        if (shouldRebind)
        {
            StopFacadeSession();
        }

        configuredFacade = facade;
        disposeConfiguredFacade = takeOwnership;

        if (isActiveAndEnabled && facade != null)
        {
            StartFacadeSession();
        }
    }

    public void ConfigureUIManager(UIManager manager)
    {
        popupHost.ConfigureUIManager(manager);
    }

    public void ReleaseConfiguredFacade()
    {
        if (facadeSessionStarted && ReferenceEquals(inventoryFacade, configuredFacade))
        {
            StopFacadeSession();
        }

        configuredFacade = null;
        disposeConfiguredFacade = false;
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

    private IInventoryUiFacade ResolveInventoryFacade(out bool ownsFacade)
    {
        if (configuredFacade != null)
        {
            ownsFacade = disposeConfiguredFacade;
            return configuredFacade;
        }

        InventoryOperateManager resolvedManager = ResolveInventoryOperateManager();
        if (resolvedManager != null)
        {
            ownsFacade = true;
            return new ManagerInventoryUiFacade(resolvedManager);
        }

        throw new MissingReferenceException($"{nameof(InventoryUI)} '{name}' requires either an externally configured {nameof(IInventoryUiFacade)} or an explicit {nameof(InventoryOperateManager)} reference.");
    }

    private void StartFacadeSession()
    {
        if (facadeSessionStarted)
        {
            return;
        }

        if (configuredFacade == null && ResolveInventoryOperateManager() == null)
        {
            return;
        }

        inventoryFacade = ResolveInventoryFacade(out ownsInventoryFacade);
        inventoryFacade.SnapshotChanged += OnSnapshotChanged;
        inventoryFacade.OperatePanelOpened += OnOperatePanelOpened;
        inventoryFacade.OperatePanelShouldClose += OnOperatePanelShouldClose;
        inventoryFacade.Activate();

        PrepareForOpen();
        inventoryFacade.RequestSnapshot();
        facadeSessionStarted = true;
    }

    private void StopFacadeSession()
    {
        if (!facadeSessionStarted)
        {
            return;
        }

        inventoryFacade.SnapshotChanged -= OnSnapshotChanged;
        inventoryFacade.OperatePanelOpened -= OnOperatePanelOpened;
        inventoryFacade.OperatePanelShouldClose -= OnOperatePanelShouldClose;
        inventoryFacade.Deactivate();

        ResetAfterClose();
        facadeSessionStarted = false;

        if (inventoryFacade != null && ownsInventoryFacade)
        {
            inventoryFacade.Dispose();
        }

        inventoryFacade = null;
        ownsInventoryFacade = false;
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
        if (string.IsNullOrEmpty(entryId) || inventoryFacade == null)
        {
            return;
        }

        currentSelectedEntryId = entryId;
        inventoryFacade.RequestOpenItemPanel(entryId);
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
        if (!IsShowingItem(entryId) || inventoryFacade == null)
        {
            return;
        }

        inventoryFacade.RequestSellItem(entryId);
    }

    private void OnMergeRequested(string entryId)
    {
        if (!IsShowingItem(entryId) || inventoryFacade == null)
        {
            return;
        }

        inventoryFacade.RequestMergeItem(entryId);
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
            inventoryFacade.RequestOpenItemPanel(popupEntryIdToRestore);
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
