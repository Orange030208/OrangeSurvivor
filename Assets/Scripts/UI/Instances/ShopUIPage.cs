using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ShopUIPage : UIPageBase
{
    [SerializeField] private ShopItemContainer shopItemPrefab;
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private UIClickTarget rerollButton;
    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("属性面板(左)")]
    [SerializeField] private UISidebarRevealMotion propertiesSidebar;
    [SerializeField] private UIClickTarget propertiesToggleButton;
    [SerializeField] private Describer propertiesDescriber;

    [Header("背包面板(右)")]
    [SerializeField] private UISidebarRevealMotion inventorySidebar;
    [SerializeField] private UIClickTarget inventoryToggleButton;

    private readonly List<ShopItemContainer> spawnedItems = new();

    private bool isPropertiesSidebarVisible = true;
    private bool isInventorySidebarVisible = true;
    private PropertiesManager propertiesManager;

    protected override void Awake()
    {
        base.Awake();
        ValidateConfiguration();
        shopItemParent.Clear();
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Subscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Subscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        BindButtonEvents();
        InjectPropertiesDependencies();
        RefreshCurrencyDisplay(FindFirstObjectByType<CurrencyWallet>());
        GameEventBus.Publish(new RequestShopSnapshotEvent());
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Unsubscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Unsubscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        UnbindPropertiesDependencies();
        UnbindButtonEvents();
        KillPanelTweens();
        ClearShopItems();
    }

    private void BindButtonEvents()
    {
        rerollButton.OnClicked += OnRerollButtonClicked;
        continueButton.OnClicked += OnContinueButtonClicked;
        propertiesToggleButton.OnClicked += OnPropertiesToggleButtonClicked;
        inventoryToggleButton.OnClicked += OnInventoryToggleButtonClicked;
    }

    private void UnbindButtonEvents()
    {
        rerollButton.OnClicked -= OnRerollButtonClicked;
        continueButton.OnClicked -= OnContinueButtonClicked;
        propertiesToggleButton.OnClicked -= OnPropertiesToggleButtonClicked;
        inventoryToggleButton.OnClicked -= OnInventoryToggleButtonClicked;
    }

    private void OnShopItemsChanged(ShopItemsChangedEvent eventData)
    {
        ClearShopItems();
        UpdateRerollDisplay(eventData);

        if (eventData.Items == null || eventData.Items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < eventData.Items.Length; i++)
        {
            SpawnShopItem(eventData.Items[i]);
        }
    }

    private void UpdateRerollDisplay(ShopItemsChangedEvent eventData)
    {
        rerollCostText.text = eventData.RerollCost.ToString();
        rerollButton.Interactable = eventData.CanReroll;
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        RefreshCurrencyDisplay(eventData.Wallet);
    }

    private void RefreshCurrencyDisplay(CurrencyWallet wallet)
    {
        if (currencyText == null)
        {
            return;
        }

        currencyText.text = wallet != null ? wallet.CurrentAmount.ToString() : "0";
    }

    private void SpawnShopItem(ShopItemData itemData)
    {
        if (shopItemPrefab == null || shopItemParent == null)
        {
            Debug.LogWarning("Shop item prefab or parent is not assigned.");
            return;
        }

        ShopItemContainer container = Instantiate(shopItemPrefab, shopItemParent);

        if (itemData.ItemData != null)
        {
            container.Configure(new InfoAddIndex<ShopItemData>(itemData, spawnedItems.Count));
        }

        spawnedItems.Add(container);
    }

    private void OnRerollButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new ShopRerollRequestedEvent());
    }

    private void OnContinueButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    private void OnPropertiesToggleButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        UIMotionAction action = isPropertiesSidebarVisible ? UIMotionAction.Hide : UIMotionAction.Show;
        propertiesSidebar.Play(action);
        isPropertiesSidebarVisible = !isPropertiesSidebarVisible;
    }

    private void OnInventoryToggleButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        UIMotionAction action = isInventorySidebarVisible ? UIMotionAction.Hide : UIMotionAction.Show;
        inventorySidebar.Play(action);
        isInventorySidebarVisible = !isInventorySidebarVisible;
    }

    private void InitSidebarPanels()
    {
        propertiesSidebar.RefreshDefaults();
        inventorySidebar.RefreshDefaults();
    }

    private void KillPanelTweens()
    {
        propertiesSidebar.Kill();
        inventorySidebar.Kill();
    }

    private void OnPurchaseSuccess(ShopPurchaseSuccessEvent eventData)
    {
        Debug.Log($"Purchase successful: {eventData.ItemData.ItemType}");
    }

    private void OnPurchaseFailed(ShopPurchaseFailedEvent eventData)
    {
        Debug.LogWarning($"Purchase failed: {eventData.Message}");
    }

    private void ClearShopItems()
    {
        foreach (ShopItemContainer item in spawnedItems)
        {
            item.CleanUp();
            Destroy(item.gameObject);
        }

        spawnedItems.Clear();
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

    private void InjectPropertiesDependencies()
    {
        UnbindPropertiesDependencies();

        Player player = FindFirstObjectByType<Player>();
        propertiesManager = player != null ? player.GetComponent<PropertiesManager>() : null;

        RefreshPropertiesDescription();

        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        }
    }

    private void UnbindPropertiesDependencies()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
            propertiesManager = null;
        }
    }

    private void OnAllPropertiesChanged()
    {
        RefreshPropertiesDescription();
    }

    private void RefreshPropertiesDescription()
    {
        if (propertiesDescriber == null)
        {
            return;
        }

        propertiesDescriber.Display(propertiesManager);
    }
}
