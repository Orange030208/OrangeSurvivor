using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIPage : PageBase
{
    private const string PROPERTIES_POPUP_BUTTON_NAME = "Arrow Right Button";
    private const string INVENTORY_POPUP_BUTTON_NAME = "Arrow Left Button";
    private const string PROPERTIES_POPUP_GROUP_ID = "shop.properties";
    private const string INVENTORY_POPUP_GROUP_ID = "shop.inventory";

    [Header("商品")]
    [SerializeField] private ShopItemListUI itemList;

    [Header("操作")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Popup 入口")]
    [SerializeField] private Button propertiesPopupButton;
    [SerializeField] private Button inventoryPopupButton;

    private ShopManager shopManager;
    private CurrencyWallet currencyWallet;
    private ShopPageContext currentContext;
    private ViewHandle<ShopPropertiesPopup> propertiesPopupHandle;
    private ViewHandle<ShopInventoryPopup> inventoryPopupHandle;
    private bool buttonEventsBound;
    private bool managerEventsBound;
    private bool propertiesPopupOpen;
    private bool inventoryPopupOpen;
    private int propertiesPopupVersion;
    private int inventoryPopupVersion;

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
        currentContext = context;

        BindButtonEvents();
        BindManagerEvents();
        BindItemListEvents();

        UpdateCurrencyAmount(context.CurrencyWallet != null ? context.CurrencyWallet.CurrentAmount : 0);
        shopManager.RefreshViewState();
    }

    private void ExitShopSession()
    {
        ClosePropertiesPopupAsync(CloseReason.Cancel).Forget();
        CloseInventoryPopupAsync(CloseReason.Cancel).Forget();

        UnbindButtonEvents();
        UnbindManagerEvents();
        UnbindItemListEvents();

        itemList.Clear();

        shopManager = null;
        currencyWallet = null;
        currentContext = null;
    }

    private void RenderShopItems(ShopItemData[] items, ShopRefreshReason reason)
    {
        itemList.Render(items, reason);
    }

    private void UpdateRerollState(int rerollCost, bool canReroll)
    {
        rerollCostText.text = rerollCost.ToString();
        rerollButton.interactable = canReroll;
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

    private void OnPropertiesPopupRequested()
    {
        TogglePropertiesPopupAsync().Forget();
    }

    private void OnInventoryPopupRequested()
    {
        ToggleInventoryPopupAsync().Forget();
    }

    private async UniTaskVoid TogglePropertiesPopupAsync()
    {
        if (propertiesPopupOpen)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await ClosePropertiesPopupAsync(CloseReason.Normal);
            return;
        }

        if (currentContext == null)
        {
            return;
        }

        int version = ++propertiesPopupVersion;
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);

        try
        {
            PopupOptions options = new PopupOptions(
                closeOnOutsideClick: true,
                groupId: PROPERTIES_POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: true,
                preferredAnchor: FloatingViewAnchor.Center);

            ViewHandle<ShopPropertiesPopup> handle = await OwnerUIManager.ShowPopupAsync<ShopPropertiesPopup>(
                new ShopPropertiesPopupContext(currentContext.PropertiesManager),
                options,
                this.GetCancellationTokenOnDestroy());

            if (version != propertiesPopupVersion || currentContext == null)
            {
                await handle.CloseAsync(CloseReason.Cancel);
                return;
            }

            propertiesPopupHandle = handle;
            propertiesPopupOpen = true;
            ObservePropertiesPopupClosedAsync(handle, version).Forget();
        }
        catch (Exception exception)
        {
            if (version == propertiesPopupVersion)
            {
                propertiesPopupOpen = false;
                propertiesPopupHandle = default;
            }

            Debug.LogException(exception, this);
        }
    }

    private async UniTaskVoid ToggleInventoryPopupAsync()
    {
        if (inventoryPopupOpen)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await CloseInventoryPopupAsync(CloseReason.Normal);
            return;
        }

        if (currentContext == null)
        {
            return;
        }

        int version = ++inventoryPopupVersion;
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);

        try
        {
            PopupOptions options = new PopupOptions(
                closeOnOutsideClick: true,
                groupId: INVENTORY_POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: true,
                preferredAnchor: FloatingViewAnchor.Center);

            ViewHandle<ShopInventoryPopup> handle = await OwnerUIManager.ShowPopupAsync<ShopInventoryPopup>(
                new ShopInventoryPopupContext(currentContext.InventoryOperateManager, OwnerUIManager),
                options,
                this.GetCancellationTokenOnDestroy());

            if (version != inventoryPopupVersion || currentContext == null)
            {
                await handle.CloseAsync(CloseReason.Cancel);
                return;
            }

            inventoryPopupHandle = handle;
            inventoryPopupOpen = true;
            ObserveInventoryPopupClosedAsync(handle, version).Forget();
        }
        catch (Exception exception)
        {
            if (version == inventoryPopupVersion)
            {
                inventoryPopupOpen = false;
                inventoryPopupHandle = default;
            }

            Debug.LogException(exception, this);
        }
    }

    private async UniTaskVoid ObservePropertiesPopupClosedAsync(ViewHandle<ShopPropertiesPopup> handle, int version)
    {
        try
        {
            await handle.ClosedTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (version != propertiesPopupVersion)
        {
            return;
        }

        propertiesPopupOpen = false;
        propertiesPopupHandle = default;
    }

    private async UniTaskVoid ObserveInventoryPopupClosedAsync(ViewHandle<ShopInventoryPopup> handle, int version)
    {
        try
        {
            await handle.ClosedTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (version != inventoryPopupVersion)
        {
            return;
        }

        inventoryPopupOpen = false;
        inventoryPopupHandle = default;
    }

    private async UniTask ClosePropertiesPopupAsync(CloseReason reason)
    {
        propertiesPopupVersion++;
        propertiesPopupOpen = false;

        ViewHandle<ShopPropertiesPopup> handle = propertiesPopupHandle;
        propertiesPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
    }

    private async UniTask CloseInventoryPopupAsync(CloseReason reason)
    {
        inventoryPopupVersion++;
        inventoryPopupOpen = false;

        ViewHandle<ShopInventoryPopup> handle = inventoryPopupHandle;
        inventoryPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
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

        rerollButton.onClick.AddListener(OnRerollRequested);
        continueButton.onClick.AddListener(OnContinueRequested);
        propertiesPopupButton.onClick.AddListener(OnPropertiesPopupRequested);
        inventoryPopupButton.onClick.AddListener(OnInventoryPopupRequested);
        buttonEventsBound = true;
    }

    private void UnbindButtonEvents()
    {
        if (!buttonEventsBound)
        {
            return;
        }

        rerollButton.onClick.RemoveListener(OnRerollRequested);
        continueButton.onClick.RemoveListener(OnContinueRequested);
        propertiesPopupButton.onClick.RemoveListener(OnPropertiesPopupRequested);
        inventoryPopupButton.onClick.RemoveListener(OnInventoryPopupRequested);
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

        if (propertiesPopupButton == null)
        {
            propertiesPopupButton = FindButtonByName(PROPERTIES_POPUP_BUTTON_NAME);
        }

        if (inventoryPopupButton == null)
        {
            inventoryPopupButton = FindButtonByName(INVENTORY_POPUP_BUTTON_NAME);
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

        if (propertiesPopupButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing properties popup button.");
        }

        if (inventoryPopupButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing inventory popup button.");
        }
    }

    private Button FindButtonByName(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }
}
