using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;

public class ShopUIPage : PageBase, IInventoryUiFacadeHost
{
    [SerializeField] private ShopItemContainer shopItemPrefab;
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private UIClickTarget rerollButton;
    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("属性面板(左)")]
    [SerializeField] private MonoBehaviour propertiesSidebar;
    [SerializeField] private UIClickTarget propertiesToggleButton;
    [SerializeField] private Describer propertiesDescriber;

    [Header("背包面板(右)")]
    [SerializeField] private MonoBehaviour inventorySidebar;
    [SerializeField] private UIClickTarget inventoryToggleButton;
    [SerializeField] private InventoryUI inventoryUI;

    private ShopPageContext currentContext;
    private ShopPageController controller;
    private ShopListView shopListView;
    private ShopSidebarHost sidebarHost;

    public event Action RerollRequested;
    public event Action ContinueRequested;
    public event Action PropertiesToggleRequested;
    public event Action InventoryToggleRequested;
    public event Action<int> ItemBuyRequested;
    public event Action<int> ItemLockToggleRequested;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiBinder.WarmUp(this, ref inventoryUI);
        shopListView = new ShopListView(name, shopItemPrefab, shopItemParent, rerollButton, continueButton, rerollCostText, currencyText);
        sidebarHost = new ShopSidebarHost(
            name,
            propertiesSidebar,
            propertiesToggleButton,
            propertiesDescriber,
            inventorySidebar,
            inventoryToggleButton);
        shopListView.RerollRequested += OnRerollRequested;
        shopListView.ContinueRequested += OnContinueRequested;
        shopListView.ItemBuyRequested += OnItemBuyRequested;
        shopListView.ItemLockToggleRequested += OnItemLockToggleRequested;
        sidebarHost.PropertiesToggleRequested += OnPropertiesToggleRequested;
        sidebarHost.InventoryToggleRequested += OnInventoryToggleRequested;
        InitSidebarPanels();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        currentContext = context.GetPayload<ShopPageContext>()
            ?? throw new InvalidOperationException($"{nameof(ShopUIPage)} requires {nameof(ShopPageContext)} payload.");
        controller = new ShopPageController(this, currentContext);
        controller.Enter();
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        controller?.Exit();
        controller = null;
        currentContext?.Dispose();
        currentContext = null;
    }

    public void PrepareForOpen(ShopPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        InventoryUiBinder.Bind(this, ref inventoryUI, context, OwnerUIManager);
        shopListView.Bind();
        sidebarHost.Bind(context.PropertiesManager);
        UpdateCurrencyAmount(context.CurrencyWallet != null ? context.CurrencyWallet.CurrentAmount : 0);
    }

    public void ResetAfterClose()
    {
        shopListView.Unbind();
        sidebarHost.Unbind();
        InventoryUiBinder.Release(inventoryUI);
        KillPanelTweens();
    }

    public void RenderShopItems(ShopItemData[] items, ShopSnapshotReason reason)
    {
        shopListView.RenderShopItems(items, reason);
    }

    public void UpdateRerollState(int rerollCost, bool canReroll)
    {
        shopListView.UpdateRerollState(rerollCost, canReroll);
    }

    public void UpdateCurrencyAmount(int amount)
    {
        shopListView.UpdateCurrencyAmount(amount);
    }

    public void ShowPurchaseSuccess(ShopPurchaseSuccess result)
    {
        Debug.Log($"Purchase successful: {result.ItemData.ItemType}");
    }

    public void ShowPurchaseFailure(string message)
    {
        Debug.LogWarning($"Purchase failed: {message}");
    }

    public void SetPropertiesSidebarVisible(bool visible)
    {
        sidebarHost.SetPropertiesVisible(visible);
    }

    public void SetInventorySidebarVisible(bool visible)
    {
        sidebarHost.SetInventoryVisible(visible);
    }

    private void OnRerollRequested()
    {
        RerollRequested?.Invoke();
    }

    private void OnContinueRequested()
    {
        ContinueRequested?.Invoke();
    }

    private void OnPropertiesToggleRequested()
    {
        PropertiesToggleRequested?.Invoke();
    }

    private void OnInventoryToggleRequested()
    {
        InventoryToggleRequested?.Invoke();
    }

    private void InitSidebarPanels()
    {
        sidebarHost.RefreshDefaults();
    }

    private void KillPanelTweens()
    {
        sidebarHost.Kill();
    }

    private void ValidateConfiguration()
    {
        if (shopItemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing shop item prefab.");
        }

        if (shopItemParent == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing shop item parent.");
        }

        if (rerollButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing reroll button.");
        }

        if (continueButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing continue button.");
        }

        if (rerollCostText == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing reroll cost text.");
        }

        if (currencyText == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing currency text.");
        }

        if (propertiesSidebar == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing properties sidebar.");
        }

        if (propertiesToggleButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing properties toggle button.");
        }

        if (propertiesDescriber == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing properties describer.");
        }

        if (inventorySidebar == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing inventory sidebar.");
        }

        if (inventoryToggleButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing inventory toggle button.");
        }
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        ItemBuyRequested?.Invoke(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        ItemLockToggleRequested?.Invoke(itemIndex);
    }
}
