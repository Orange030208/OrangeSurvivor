using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;

public class ShopUIPage : PageBase
{
    [Header("商品")]
    [SerializeField] private ShopItemListUI itemList;

    [Header("操作")]
    [SerializeField] private UIClickTarget rerollButton;
    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("页面子面板")]
    [SerializeField] private ShopPropertiesPanel propertiesPanel;
    [SerializeField] private ShopInventoryPanel inventoryPanel;

    private ShopManager shopManager;
    private CurrencyWallet currencyWallet;
    private bool buttonEventsBound;
    private bool managerEventsBound;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ShopPageContext shopPageContext = context.GetPayload<ShopPageContext>()
            ?? throw new InvalidOperationException($"{nameof(ShopUIPage)} requires {nameof(ShopPageContext)} payload.");

        EnterShopSession(shopPageContext);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        ExitShopSession();
    }

    private void EnterShopSession(ShopPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        shopManager = context.ShopManager;
        currencyWallet = context.CurrencyWallet;

        BindButtonEvents();
        BindManagerEvents();
        BindItemListEvents();

        propertiesPanel.BeginSession(context.PropertiesManager);
        inventoryPanel.BeginSession(context.InventoryOperateManager, OwnerUIManager);

        UpdateCurrencyAmount(context.CurrencyWallet != null ? context.CurrencyWallet.CurrentAmount : 0);
        shopManager.RefreshViewState();
    }

    private void ExitShopSession()
    {
        UnbindButtonEvents();
        UnbindManagerEvents();
        UnbindItemListEvents();

        itemList.Clear();
        propertiesPanel.EndSession();
        inventoryPanel.EndSession();

        shopManager = null;
        currencyWallet = null;
    }

    private void RenderShopItems(ShopItemData[] items, ShopRefreshReason reason)
    {
        itemList.Render(items, reason);
    }

    private void UpdateRerollState(int rerollCost, bool canReroll)
    {
        rerollCostText.text = rerollCost.ToString();
        rerollButton.Interactable = canReroll;
    }

    private void UpdateCurrencyAmount(int amount)
    {
        currencyText.text = amount.ToString();
    }

    private void ShowPurchaseSuccess(ShopPurchaseSuccess result)
    {
        Debug.Log($"Purchase successful: {result.ItemData.ItemType}");
    }

    private void ShowPurchaseFailure(string message)
    {
        Debug.LogWarning($"Purchase failed: {message}");
    }

    private void OnRerollRequested()
    {
        shopManager?.RequestReroll();
    }

    private void OnContinueRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        shopManager?.RequestBuyItem(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        shopManager?.RequestToggleLock(itemIndex);
    }

    private void BindButtonEvents()
    {
        if (buttonEventsBound)
        {
            return;
        }

        rerollButton.OnClicked += OnRerollRequested;
        continueButton.OnClicked += OnContinueRequested;
        buttonEventsBound = true;
    }

    private void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

        rerollButton.OnClicked -= OnRerollRequested;
        continueButton.OnClicked -= OnContinueRequested;
        buttonEventsBound = false;
    }

    private void BindManagerEvents()
    {
        if (managerEventsBound)
        {
            return;
        }

        if (shopManager != null)
        {
            shopManager.ViewStateChanged += OnViewStateChanged;
            shopManager.PurchaseSucceeded += OnPurchaseSucceeded;
            shopManager.PurchaseFailed += OnPurchaseFailed;
        }

        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        managerEventsBound = true;
    }

    private void UnbindManagerEvents()
    {
        if (!managerEventsBound)
        {
            return;
        }

        if (shopManager != null)
        {
            shopManager.ViewStateChanged -= OnViewStateChanged;
            shopManager.PurchaseSucceeded -= OnPurchaseSucceeded;
            shopManager.PurchaseFailed -= OnPurchaseFailed;
        }

        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        managerEventsBound = false;
    }

    private void BindItemListEvents()
    {
        itemList.BuyRequested -= OnItemBuyRequested;
        itemList.LockToggleRequested -= OnItemLockToggleRequested;
        itemList.BuyRequested += OnItemBuyRequested;
        itemList.LockToggleRequested += OnItemLockToggleRequested;
    }

    private void UnbindItemListEvents()
    {
        itemList.BuyRequested -= OnItemBuyRequested;
        itemList.LockToggleRequested -= OnItemLockToggleRequested;
    }

    private void OnViewStateChanged(ShopViewState viewState)
    {
        UpdateRerollState(viewState.RerollCost, viewState.CanReroll);
        RenderShopItems(viewState.Items, viewState.Reason);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccess result)
    {
        ShowPurchaseSuccess(result);
    }

    private void OnPurchaseFailed(ShopPurchaseFailure failure)
    {
        ShowPurchaseFailure(failure.Message);
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        if (currencyWallet != null && eventData.Wallet != currencyWallet)
        {
            return;
        }

        UpdateCurrencyAmount(eventData.CurrentAmount);
    }

    private void ResolveViewParts()
    {
        if (itemList == null)
        {
            itemList = GetComponentInChildren<ShopItemListUI>(true);
        }

        if (propertiesPanel == null)
        {
            propertiesPanel = GetComponentInChildren<ShopPropertiesPanel>(true);
        }

        if (inventoryPanel == null)
        {
            inventoryPanel = GetComponentInChildren<ShopInventoryPanel>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (itemList == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing item list.");
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

        if (propertiesPanel == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing properties panel.");
        }

        if (inventoryPanel == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing inventory panel.");
        }
    }
}
