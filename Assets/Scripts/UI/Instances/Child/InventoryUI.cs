using Orange.UIFramework;
using UnityEngine;

public class InventoryUI : MonoBehaviour, IInventoryRegionView
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
    private bool requiresExternalFacadeConfiguration;
    private InventoryRegionController controller;
    private InventoryListRegionView listRegion;
    private InventoryPopupHostView popupHost;

    public event System.Action<string> ItemSelected;
    public event System.Action CloseRequested;
    public event System.Action<string> SellRequested;
    public event System.Action<string> MergeRequested;

    private void Awake()
    {
        ValidateConfiguration();
        requiresExternalFacadeConfiguration = ResolveRequiresExternalFacadeConfiguration();
        listRegion = new InventoryListRegionView(name, itemPrefab, itemContainersParent);
        popupHost = new InventoryPopupHostView(name);
        listRegion.ItemClicked += OnItemSelected;
        popupHost.CloseRequested += OnCloseRequested;
        popupHost.SellRequested += OnSellRequested;
        popupHost.MergeRequested += OnMergeRequested;
    }

    private void OnEnable()
    {
        StartController();
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
            CloseRequested?.Invoke();
        }
    }

    private void OnDisable()
    {
        StopController();
    }

    public void ConfigureFacade(IInventoryUiFacade facade, bool takeOwnership = false)
    {
        if (configuredFacade == facade && disposeConfiguredFacade == takeOwnership)
        {
            if (controller == null && isActiveAndEnabled && facade != null)
            {
                StartController();
            }

            return;
        }

        bool shouldRebind = controller != null && isActiveAndEnabled;
        if (shouldRebind)
        {
            StopController();
        }

        configuredFacade = facade;
        disposeConfiguredFacade = takeOwnership;

        if (isActiveAndEnabled && facade != null)
        {
            StartController();
        }
    }

    public void ReleaseConfiguredFacade()
    {
        if (controller != null && ReferenceEquals(inventoryFacade, configuredFacade))
        {
            StopController();
        }

        configuredFacade = null;
        disposeConfiguredFacade = false;
    }

    public void PrepareForOpen()
    {
        popupHost.CloseCurrent();
    }

    public void ResetAfterClose()
    {
        listRegion.Clear();
        popupHost.CloseCurrent();
    }

    public void RenderItems(InventoryUIItemSnapshot[] items)
    {
        listRegion.Render(items);
    }

    public void ShowOperatePopup(InventoryItemOperateResource resource)
    {
        popupHost.Show(resource);
    }

    public void CloseOperatePopup()
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

    private void StartController()
    {
        if (controller != null)
        {
            return;
        }

        if (requiresExternalFacadeConfiguration && configuredFacade == null)
        {
            return;
        }

        inventoryFacade = ResolveInventoryFacade(out ownsInventoryFacade);
        controller = new InventoryRegionController(this, inventoryFacade);
        controller.Enter();
    }

    private void StopController()
    {
        controller?.Exit();
        controller = null;

        if (inventoryFacade != null && ownsInventoryFacade)
        {
            inventoryFacade.Dispose();
        }

        inventoryFacade = null;
        ownsInventoryFacade = false;
    }

    private bool ResolveRequiresExternalFacadeConfiguration()
    {
        MonoBehaviour[] parentBehaviours = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < parentBehaviours.Length; i++)
        {
            if (parentBehaviours[i] is IInventoryUiFacadeHost)
            {
                return true;
            }
        }

        return false;
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
        ItemSelected?.Invoke(entryId);
    }

    private void OnCloseRequested()
    {
        CloseRequested?.Invoke();
    }

    private void OnSellRequested(string entryId)
    {
        SellRequested?.Invoke(entryId);
    }

    private void OnMergeRequested(string entryId)
    {
        MergeRequested?.Invoke(entryId);
    }
}
