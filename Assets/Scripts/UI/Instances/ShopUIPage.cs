using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ShopUIPage : PageBase
{
    private const string PROPERTIES_POPUP_GROUP_ID = "properties";
    private const string EQUIPMENT_POPUP_GROUP_ID = "equipment";
    private const string PURCHASE_INSUFFICIENT_CURRENCY_MESSAGE = "Not enough currency.";
    private const string REROLL_INSUFFICIENT_CURRENCY_PREFIX = "Not enough currency for reroll";
    private const string FREE_REROLL_ZERO_TEXT = "<size=135%><color=#FFD84A><b>0</b></color></size>";

    [Header("商品")]
    [SerializeField] private ShopItemListUI itemList;

    [Header("操作")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Popup 入口")]
    [SerializeField] private Button propertiesPopupButton;
    [FormerlySerializedAs("inventoryPopupButton")]
    [SerializeField] private Button equipmentPopupButton;

    private IShopController shopController;
    private CurrencyWallet currencyWallet;
    private ShopPageContext currentContext;
    private ViewHandle<PropertiesPopup> propertiesPopupHandle;
    private ViewHandle<EquipmentPopup> equipmentPopupHandle;
    private bool buttonEventsBound;
    private bool managerEventsBound;
    private bool propertiesPopupOpen;
    private bool equipmentPopupOpen;
    private int propertiesPopupVersion;
    private int equipmentPopupVersion;

    protected override void OnCreate()
    {
        base.OnCreate();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ShopPageContext shopPageContext = context.GetPayload<ShopPageContext>()
            ?? throw new InvalidOperationException($"{nameof(ShopUIPage)} requires {nameof(ShopPageContext)} payload.");

        OnEnterShop(shopPageContext);
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        OnExitShop();
    }

    private void OnEnterShop(ShopPageContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        shopController = context.ShopController;
        currentContext = context;

        BindButtonEvents();
        BindManagerEvents();
        BindCurrencyWallet(context.CurrencyWallet);
        BindItemListEvents();
        shopController.RefreshViewState();
    }

    private void OnExitShop()
    {
        ClosePropertiesPopupAsync(CloseReason.Cancel).Forget();
        CloseEquipmentPopupAsync(CloseReason.Cancel).Forget();

        UnbindButtonEvents();
        UnbindManagerEvents();
        UnbindCurrencyWallet();
        UnbindItemListEvents();

        itemList.Clear();

        shopController = null;
        currencyWallet = null;
        currentContext = null;
    }

    private void RenderShopItems(ShopItemData[] items, ShopRefreshReason reason)
    {
        itemList.Render(items, reason);
    }

    private void UpdateRerollState(int rerollCost, int freeRerollCount, bool canReroll)
    {
        rerollCostText.richText = true;
        rerollCostText.text = BuildRerollCostText(rerollCost, freeRerollCount);
        rerollButton.interactable = canReroll;
    }

    private void UpdateCurrencyAmount(int amount)
    {
        currencyText.text = amount.ToString();
    }

    private static string BuildRerollCostText(int rerollCost, int freeRerollCount)
    {
        if (freeRerollCount <= 0)
        {
            return rerollCost.ToString();
        }

        string remainingText = freeRerollCount > 1
            ? $" <size=70%><color=#FFEBA0>x{freeRerollCount}</color></size>"
            : string.Empty;
        return $"<s><color=#8F8F8F>{rerollCost}</color></s>  {FREE_REROLL_ZERO_TEXT}{remainingText}";
    }

    private void ShowPurchaseSuccess(ShopPurchaseSuccess result)
    {
        ItemDataSO itemData = result.ItemData;
        string itemName = itemData != null && !string.IsNullOrWhiteSpace(itemData.ItemName)
            ? itemData.ItemName
            : "商品";

        Debug.Log($"[Shop] 购买成功：{itemName}", this);
    }

    private void ShowPurchaseFailure(string message)
    {
        string feedbackMessage = BuildPurchaseFailureFeedbackMessage(message);
        if (IsPurchaseInsufficientCurrency(message))
        {
            ShowToast(feedbackMessage);
            return;
        }

        Debug.LogWarning($"[Shop] {feedbackMessage}", this);
    }

    private void OnRerollRequested()
    {
        shopController?.RequestReroll();
    }

    private void OnContinueRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
        YokiFrame.EventKit.Enum.Send(GameFlowCommand.ShopContinueClicked);
    }

    private void OnPropertiesPopupRequested()
    {
        TogglePropertiesPopupAsync().Forget();
    }

    private void OnEquipmentPopupRequested()
    {
        ToggleEquipmentPopupAsync().Forget();
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

            ViewHandle<PropertiesPopup> handle = await UIManager.Instance.ShowPopupAsync<PropertiesPopup>(
                currentContext.AttributeManager,
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

    private async UniTaskVoid ToggleEquipmentPopupAsync()
    {
        if (equipmentPopupOpen)
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.UiCancel);
            await CloseEquipmentPopupAsync(CloseReason.Normal);
            return;
        }

        if (currentContext == null || !TryCreateEquipmentContext(currentContext.Player, out EquipmentPopupContext equipmentContext))
        {
            return;
        }

        int version = ++equipmentPopupVersion;
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);

        try
        {
            PopupOptions options = new PopupOptions(
                closeOnOutsideClick: false,
                groupId: EQUIPMENT_POPUP_GROUP_ID,
                replaceSameGroup: true,
                trackInStack: true,
                preferredAnchor: FloatingViewAnchor.Center);

            ViewHandle<EquipmentPopup> handle = await UIManager.Instance.ShowPopupAsync<EquipmentPopup>(
                equipmentContext,
                options,
                this.GetCancellationTokenOnDestroy());

            if (version != equipmentPopupVersion || currentContext == null)
            {
                await handle.CloseAsync(CloseReason.Cancel);
                return;
            }

            equipmentPopupHandle = handle;
            equipmentPopupOpen = true;
            ObserveEquipmentPopupClosedAsync(handle, version).Forget();
        }
        catch (Exception exception)
        {
            if (version == equipmentPopupVersion)
            {
                equipmentPopupOpen = false;
                equipmentPopupHandle = default;
            }

            Debug.LogException(exception, this);
        }
    }

    private async UniTaskVoid ObservePropertiesPopupClosedAsync(ViewHandle<PropertiesPopup> handle, int version)
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

    private async UniTaskVoid ObserveEquipmentPopupClosedAsync(ViewHandle<EquipmentPopup> handle, int version)
    {
        try
        {
            await handle.ClosedTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (version != equipmentPopupVersion)
        {
            return;
        }

        equipmentPopupOpen = false;
        equipmentPopupHandle = default;
    }

    private async UniTask ClosePropertiesPopupAsync(CloseReason reason)
    {
        propertiesPopupVersion++;
        propertiesPopupOpen = false;

        ViewHandle<PropertiesPopup> handle = propertiesPopupHandle;
        propertiesPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
    }

    private async UniTask CloseEquipmentPopupAsync(CloseReason reason)
    {
        equipmentPopupVersion++;
        equipmentPopupOpen = false;

        ViewHandle<EquipmentPopup> handle = equipmentPopupHandle;
        equipmentPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(reason);
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        shopController?.RequestBuyItem(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        shopController?.RequestToggleLock(itemIndex);
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
        equipmentPopupButton.onClick.AddListener(OnEquipmentPopupRequested);
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
        equipmentPopupButton.onClick.RemoveListener(OnEquipmentPopupRequested);
        buttonEventsBound = false;
    }

    private void BindManagerEvents()
    {
        if (managerEventsBound)
        {
            return;
        }

        if (shopController != null)
        {
            shopController.ViewStateChanged += OnViewStateChanged;
            shopController.PurchaseSucceeded += OnPurchaseSucceeded;
            shopController.PurchaseFailed += OnPurchaseFailed;
        }
        managerEventsBound = true;
    }

    private void UnbindManagerEvents()
    {
        if (!managerEventsBound)
        {
            return;
        }

        if (shopController != null)
        {
            shopController.ViewStateChanged -= OnViewStateChanged;
            shopController.PurchaseSucceeded -= OnPurchaseSucceeded;
            shopController.PurchaseFailed -= OnPurchaseFailed;
        }
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
        UpdateRerollState(viewState.RerollCost, viewState.FreeRerollCount, viewState.CanReroll);
        RenderShopItems(viewState.Items, viewState.Reason);
        LogRefresh(viewState.Reason);
    }

    private void OnPurchaseSucceeded(ShopPurchaseSuccess result)
    {
        ShowPurchaseSuccess(result);
    }

    private void OnPurchaseFailed(ShopPurchaseFailure failure)
    {
        ShowPurchaseFailure(failure.Message);
    }

    private void OnCurrencyAmountChanged(int currentAmount, int changeAmount)
    {
        UpdateCurrencyAmount(currentAmount);
    }

    private void BindCurrencyWallet(CurrencyWallet newCurrencyWallet)
    {
        UnbindCurrencyWallet();
        currencyWallet = newCurrencyWallet;

        if (currencyWallet != null)
        {
            currencyWallet.OnAmountChanged += OnCurrencyAmountChanged;
            UpdateCurrencyAmount(currencyWallet.CurrentAmount);
            return;
        }

        UpdateCurrencyAmount(0);
    }

    private void UnbindCurrencyWallet()
    {
        if (currencyWallet == null)
        {
            return;
        }

        currencyWallet.OnAmountChanged -= OnCurrencyAmountChanged;
        currencyWallet = null;
    }

    private void LogRefresh(ShopRefreshReason reason)
    {
        switch (reason)
        {
            case ShopRefreshReason.Reroll:
                Debug.Log("[Shop] 商店已刷新", this);
                break;
            case ShopRefreshReason.WaveRefresh:
                Debug.Log("[Shop] 新一轮商店已刷新", this);
                break;
        }
    }

    private void ShowToast(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        UIManager.Instance.ShowToastAsync<TextToastView>(
            new ToastPayload(message),
            new ToastOptions(displayMode: ToastDisplayMode.ReplaceCurrent),
            cancellationToken: this.GetCancellationTokenOnDestroy()).Forget();
    }

    private static bool IsPurchaseInsufficientCurrency(string message)
    {
        return string.Equals(message, PURCHASE_INSUFFICIENT_CURRENCY_MESSAGE, StringComparison.Ordinal);
    }

    private static string BuildPurchaseFailureFeedbackMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "购买失败";
        }

        if (message.StartsWith(REROLL_INSUFFICIENT_CURRENCY_PREFIX, StringComparison.Ordinal))
        {
            return "金币不足，无法刷新商店";
        }

        return message switch
        {
            "Item index out of range." => "商品已失效",
            "Item data is null." => "商品数据异常，无法购买",
            "Accessory data is null or wrong type." => "饰品数据异常，无法购买",
            "Weapon data is null or wrong type." => "武器数据异常，无法购买",
            PURCHASE_INSUFFICIENT_CURRENCY_MESSAGE => "金币不足，无法购买",
            "Accessory manager not found." => "当前角色无法装备饰品",
            "Weapons holder not found." => "当前角色无法装备武器",
            "Accessory owned limit reached." => "饰品数量已达上限",
            "No empty weapon slot available." => "武器栏已满",
            _ => "购买失败"
        };
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

        if (equipmentPopupButton == null)
        {
            throw new MissingReferenceException($"{nameof(ShopUIPage)} '{name}' is missing equipment popup button.");
        }
    }

    private static bool TryCreateEquipmentContext(Player player, out EquipmentPopupContext equipmentContext)
    {
        equipmentContext = null;
        if (player == null)
        {
            return false;
        }

        WeaponsHolder weaponsHolder = player.GetComponent<WeaponsHolder>();
        AccessoryManager accessoryManager = player.GetComponent<AccessoryManager>();
        CurrencyWallet wallet = player.GetComponent<CurrencyWallet>();
        if (weaponsHolder == null || accessoryManager == null || wallet == null)
        {
            return false;
        }

        equipmentContext = new EquipmentPopupContext(weaponsHolder, accessoryManager, wallet);
        return true;
    }
}
