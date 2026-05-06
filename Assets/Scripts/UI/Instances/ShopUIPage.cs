using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIPage : PageBase
{
    private const float LAYOUT_MOVE_DURATION = 0.18f;

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

    private readonly List<ShopItemContainer> renderedItems = new();
    private readonly List<ShopItemIdentity> renderedItemIdentities = new();
    private readonly Dictionary<ShopItemContainer, Tween> layoutMoveTweens = new();

    private ShopManager shopManager;
    private CurrencyWallet currencyWallet;
    private PropertiesManager propertiesManager;
    private IUIRuntimeMotion propertiesRuntimeMotion;
    private IUIRuntimeMotion inventoryRuntimeMotion;
    private bool isPropertiesSidebarVisible;
    private bool isInventorySidebarVisible;
    private bool buttonEventsBound;
    private bool managerEventsBound;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        InventoryUiBinder.WarmUp(this, ref inventoryUI);
        propertiesRuntimeMotion = ResolveRuntimeMotion(propertiesSidebar, "properties sidebar");
        inventoryRuntimeMotion = ResolveRuntimeMotion(inventorySidebar, "inventory sidebar");
        shopItemParent.Clear();
        InitSidebarPanels();
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
        InventoryUiBinder.Bind(this, ref inventoryUI, context.InventoryOperateManager, OwnerUIManager);
        BindButtonEvents();
        BindManagerEvents();
        BindPropertiesManager(context.PropertiesManager);

        isPropertiesSidebarVisible = false;
        isInventorySidebarVisible = false;
        SetPropertiesSidebarVisible(isPropertiesSidebarVisible);
        SetInventorySidebarVisible(isInventorySidebarVisible);
        UpdateCurrencyAmount(context.CurrencyWallet != null ? context.CurrencyWallet.CurrentAmount : 0);
        shopManager.RequestSnapshot();
    }

    private void ExitShopSession()
    {
        UnbindButtonEvents();
        UnbindManagerEvents();
        BindPropertiesManager(null);
        ClearShopItems();
        InventoryUiBinder.Release(inventoryUI);
        KillPanelTweens();
        propertiesRuntimeMotion.SetImmediate(UIMotionClipIds.HIDE);
        inventoryRuntimeMotion.SetImmediate(UIMotionClipIds.HIDE);
        isPropertiesSidebarVisible = false;
        isInventorySidebarVisible = false;
        shopManager = null;
        currencyWallet = null;
        propertiesManager = null;
    }

    private void RenderShopItems(ShopItemData[] items, ShopSnapshotReason reason)
    {
        RenderShopItemList(items, reason);
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

    private void SetPropertiesSidebarVisible(bool visible)
    {
        propertiesRuntimeMotion?.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void SetInventorySidebarVisible(bool visible)
    {
        inventoryRuntimeMotion?.Play(visible ? UIMotionClipIds.SHOW : UIMotionClipIds.HIDE);
    }

    private void OnRerollRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        shopManager?.RequestReroll();
    }

    private void OnContinueRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    private void OnPropertiesToggleRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        isPropertiesSidebarVisible = !isPropertiesSidebarVisible;
        SetPropertiesSidebarVisible(isPropertiesSidebarVisible);
    }

    private void OnInventoryToggleRequested()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        isInventorySidebarVisible = !isInventorySidebarVisible;
        SetInventorySidebarVisible(isInventorySidebarVisible);
    }

    private void InitSidebarPanels()
    {
        propertiesRuntimeMotion.RefreshDefaults();
        inventoryRuntimeMotion.RefreshDefaults();
    }

    private void KillPanelTweens()
    {
        propertiesRuntimeMotion.Kill();
        inventoryRuntimeMotion.Kill();
        KillAllLayoutMoveTweens();
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
        propertiesToggleButton.OnClicked += OnPropertiesToggleRequested;
        inventoryToggleButton.OnClicked += OnInventoryToggleRequested;
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
        propertiesToggleButton.OnClicked -= OnPropertiesToggleRequested;
        inventoryToggleButton.OnClicked -= OnInventoryToggleRequested;
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
            shopManager.ItemsChanged += OnSnapshotChanged;
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
            shopManager.ItemsChanged -= OnSnapshotChanged;
            shopManager.PurchaseSucceeded -= OnPurchaseSucceeded;
            shopManager.PurchaseFailed -= OnPurchaseFailed;
        }

        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        managerEventsBound = false;
    }

    private void BindPropertiesManager(PropertiesManager manager)
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= OnPropertiesChanged;
        }

        propertiesManager = manager;
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += OnPropertiesChanged;
        }

        RefreshPropertiesDisplay();
    }

    private void OnSnapshotChanged(ShopSnapshot snapshot)
    {
        UpdateRerollState(snapshot.RerollCost, snapshot.CanReroll);
        RenderShopItems(snapshot.Items, snapshot.Reason);
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

    private void OnPropertiesChanged()
    {
        RefreshPropertiesDisplay();
    }

    private void RefreshPropertiesDisplay()
    {
        propertiesDescriber.Display(propertiesManager);
    }

    private void RenderShopItemList(ShopItemData[] items, ShopSnapshotReason reason)
    {
        if (items == null || items.Length == 0)
        {
            ClearShopItems();
            return;
        }

        List<ShopItemContainer> previousItems = new(renderedItems);
        List<ShopItemIdentity> previousIdentities = new(renderedItemIdentities);
        Dictionary<ShopItemContainer, Vector2> previousPositions = CaptureAnchoredPositions(previousItems);
        bool[] previousItemConsumed = new bool[previousItems.Count];
        List<LayoutMoveRequest> layoutMoveRequests = new();

        renderedItems.Clear();
        renderedItemIdentities.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            RenderShopItem(
                items[i],
                i,
                reason,
                previousItems,
                previousIdentities,
                previousPositions,
                previousItemConsumed,
                layoutMoveRequests);
        }

        DestroyUnusedPreviousItems(previousItems, previousItemConsumed);
        PlayLayoutMoveAnimations(layoutMoveRequests);
    }

    private void ClearShopItems()
    {
        KillAllLayoutMoveTweens();
        for (int i = 0; i < renderedItems.Count; i++)
        {
            DestroyShopItem(renderedItems[i]);
        }

        renderedItems.Clear();
        renderedItemIdentities.Clear();
    }

    private void RenderShopItem(
        ShopItemData itemData,
        int itemIndex,
        ShopSnapshotReason reason,
        List<ShopItemContainer> previousItems,
        List<ShopItemIdentity> previousIdentities,
        Dictionary<ShopItemContainer, Vector2> previousPositions,
        bool[] previousItemConsumed,
        List<LayoutMoveRequest> layoutMoveRequests)
    {
        if (itemData.ItemData == null)
        {
            Debug.LogWarning($"{nameof(ShopUIPage)} on '{name}' skipped rendering a shop item without {nameof(ItemDataSO)}.", this);
            return;
        }

        ShopItemIdentity nextIdentity = ShopItemIdentity.From(itemData);
        int reusableItemIndex = FindReusableItemIndex(nextIdentity, previousItems, previousIdentities, previousItemConsumed);
        bool reusedExistingItem = reusableItemIndex >= 0;
        bool playReveal = ShouldPlayReveal(itemData, reason, reusedExistingItem);
        ShopItemContainer container = reusedExistingItem
            ? previousItems[reusableItemIndex]
            : CreateShopItem();

        if (reusedExistingItem)
        {
            previousItemConsumed[reusableItemIndex] = true;
        }

        container.transform.SetSiblingIndex(itemIndex);
        bool refreshMotion = !reusedExistingItem || playReveal;
        container.Configure(new InfoAddIndex<ShopItemData>(itemData, itemIndex), playReveal, refreshMotion);
        if (!playReveal
            && reusedExistingItem
            && previousPositions.TryGetValue(container, out Vector2 previousAnchoredPosition))
        {
            layoutMoveRequests.Add(new LayoutMoveRequest(container, previousAnchoredPosition));
        }

        renderedItems.Add(container);
        renderedItemIdentities.Add(nextIdentity);
    }

    private ShopItemContainer CreateShopItem()
    {
        ShopItemContainer container = Instantiate(shopItemPrefab, shopItemParent);
        BindShopItemCallbacks(container);
        return container;
    }

    private void DestroyUnusedPreviousItems(List<ShopItemContainer> previousItems, bool[] previousItemConsumed)
    {
        for (int i = 0; i < previousItems.Count; i++)
        {
            if (i < previousItemConsumed.Length && previousItemConsumed[i])
            {
                continue;
            }

            DestroyShopItem(previousItems[i]);
        }
    }

    private void DestroyShopItem(ShopItemContainer item)
    {
        if (item == null)
        {
            return;
        }

        KillLayoutMoveTween(item, complete: false);
        UnbindShopItemCallbacks(item);
        item.CleanUp();
        Destroy(item.gameObject);
    }

    private Dictionary<ShopItemContainer, Vector2> CaptureAnchoredPositions(List<ShopItemContainer> items)
    {
        Dictionary<ShopItemContainer, Vector2> positions = new();
        for (int i = 0; i < items.Count; i++)
        {
            ShopItemContainer item = items[i];
            RectTransform rectTransform = GetRectTransform(item);
            if (item == null || rectTransform == null)
            {
                continue;
            }

            positions[item] = rectTransform.anchoredPosition;
        }

        return positions;
    }

    private void PlayLayoutMoveAnimations(List<LayoutMoveRequest> layoutMoveRequests)
    {
        if (layoutMoveRequests.Count == 0)
        {
            return;
        }

        RectTransform parentRectTransform = shopItemParent as RectTransform;
        if (parentRectTransform == null)
        {
            return;
        }

        // LayoutGroup 仍然负责最终排布；这里先强制算出目标位置，再把复用卡片从旧位置补间过去。
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < layoutMoveRequests.Count; i++)
        {
            PlayLayoutMoveAnimation(layoutMoveRequests[i]);
        }
    }

    private void PlayLayoutMoveAnimation(LayoutMoveRequest request)
    {
        RectTransform rectTransform = GetRectTransform(request.Container);
        if (rectTransform == null)
        {
            return;
        }

        Vector2 targetAnchoredPosition = rectTransform.anchoredPosition;
        if ((targetAnchoredPosition - request.PreviousAnchoredPosition).sqrMagnitude < 0.01f)
        {
            return;
        }

        KillLayoutMoveTween(request.Container, complete: false);
        rectTransform.anchoredPosition = request.PreviousAnchoredPosition;
        Tween tween = rectTransform
            .DOAnchorPos(targetAnchoredPosition, LAYOUT_MOVE_DURATION)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnKill(() => layoutMoveTweens.Remove(request.Container));

        layoutMoveTweens[request.Container] = tween;
    }

    private void KillAllLayoutMoveTweens()
    {
        List<ShopItemContainer> items = new(layoutMoveTweens.Keys);
        for (int i = 0; i < items.Count; i++)
        {
            KillLayoutMoveTween(items[i], complete: false);
        }

        layoutMoveTweens.Clear();
    }

    private void KillLayoutMoveTween(ShopItemContainer item, bool complete)
    {
        if (item == null || !layoutMoveTweens.TryGetValue(item, out Tween tween))
        {
            return;
        }

        tween?.Kill(complete);
        layoutMoveTweens.Remove(item);
    }

    private int FindReusableItemIndex(
        ShopItemIdentity identity,
        List<ShopItemContainer> previousItems,
        List<ShopItemIdentity> previousIdentities,
        bool[] previousItemConsumed)
    {
        int count = Mathf.Min(previousItems.Count, previousIdentities.Count);
        for (int i = 0; i < count; i++)
        {
            if (previousItemConsumed[i] || previousItems[i] == null)
            {
                continue;
            }

            if (previousIdentities[i].Equals(identity))
            {
                return i;
            }
        }

        return -1;
    }

    private void BindShopItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested += OnItemBuyRequested;
        container.LockToggleRequested += OnItemLockToggleRequested;
    }

    private void UnbindShopItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested -= OnItemBuyRequested;
        container.LockToggleRequested -= OnItemLockToggleRequested;
    }

    private IUIRuntimeMotion ResolveRuntimeMotion(MonoBehaviour source, string fieldName)
    {
        if (source is IUIRuntimeMotion directMotion)
        {
            return directMotion;
        }

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIRuntimeMotion motion)
            {
                return motion;
            }
        }

        throw new MissingComponentException($"{nameof(ShopUIPage)} '{name}' expects {fieldName} to implement {nameof(IUIRuntimeMotion)}.");
    }

    private static RectTransform GetRectTransform(ShopItemContainer item)
    {
        return item != null ? item.transform as RectTransform : null;
    }

    private static bool ShouldPlayReveal(
        ShopItemData itemData,
        ShopSnapshotReason reason,
        bool reusedExistingItem)
    {
        if (reason == ShopSnapshotReason.Reroll || reason == ShopSnapshotReason.WaveRefresh)
        {
            return !itemData.Lock;
        }

        return !reusedExistingItem;
    }

    private readonly struct ShopItemIdentity : IEquatable<ShopItemIdentity>
    {
        private readonly ItemDataSO itemData;
        private readonly int level;

        private ShopItemIdentity(ItemDataSO itemData, int level)
        {
            this.itemData = itemData;
            this.level = level;
        }

        public static ShopItemIdentity From(ShopItemData itemData)
        {
            return new ShopItemIdentity(itemData.ItemData, itemData.Level);
        }

        public bool Equals(ShopItemIdentity other)
        {
            return itemData == other.itemData && level == other.level;
        }
    }

    private readonly struct LayoutMoveRequest
    {
        public readonly ShopItemContainer Container;
        public readonly Vector2 PreviousAnchoredPosition;

        public LayoutMoveRequest(ShopItemContainer container, Vector2 previousAnchoredPosition)
        {
            Container = container;
            PreviousAnchoredPosition = previousAnchoredPosition;
        }
    }
}
