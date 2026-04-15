using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUIPage : UIPageBase
{
    [SerializeField] private ShopItemContainer shopItemPrefab;
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private UIClickTarget showPropertiesButton;
    [SerializeField] private UIClickTarget showInventoryButton;
    [SerializeField] private UIClickTarget rerollButton;
    [SerializeField] private UIClickTarget watchVideoRerollButton;
    [SerializeField] private UIClickTarget continueButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;

    [Header("属性面板(左)")] [SerializeField] private UISidebarRevealMotion propertiesSidebar;
    [SerializeField] private UIPropertiesViewSync propertiesViewSync;

    [Header("背包面板(右)")] [SerializeField] private UISidebarRevealMotion inventorySidebar;

    [Header("侧边遮罩")] [SerializeField] private UIClickTarget closeSidebarButton;

    private readonly List<ShopItemContainer> spawnedItems = new();

    private UISidebarRevealMotion currentOpenPanel;

    protected override void Awake()
    {
        base.Awake();
        shopItemParent.Clear();
        InitSidebarPanels();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Subscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Subscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);

        BindButtonEvents();
        InjectPropertiesDependencies();
        propertiesViewSync.StartSync();
        HideAllSidebarPanelsImmediately();
        GameEventBus.Publish(new RequestShopSnapshotEvent());
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Unsubscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Unsubscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);

        UnbindButtonEvents();
        propertiesViewSync.StopSync();
        KillPanelTweens();
        ClearShopItems();
    }

    private void BindButtonEvents()
    {
        BindClick(rerollButton, OnRerollButtonClicked);
        BindClick(watchVideoRerollButton, OnWatchVideoRerollButtonClicked);
        BindClick(continueButton, OnContinueButtonClicked);
        BindClick(showPropertiesButton, DisplayProperties);
        BindClick(showInventoryButton, DisplayInventory);
        BindClick(closeSidebarButton, OnCloseSidebarClicked);
    }

    private void UnbindButtonEvents()
    {
        UnbindClick(rerollButton, OnRerollButtonClicked);
        UnbindClick(watchVideoRerollButton, OnWatchVideoRerollButtonClicked);
        UnbindClick(continueButton, OnContinueButtonClicked);
        UnbindClick(showPropertiesButton, DisplayProperties);
        UnbindClick(showInventoryButton, DisplayInventory);
        UnbindClick(closeSidebarButton, OnCloseSidebarClicked);
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
        GameEventBus.Publish(new ShopRerollRequestedEvent());
    }

    private void OnWatchVideoRerollButtonClicked()
    {
        GameEventBus.Publish(new ShopVideoAdRerollRequestedEvent());
    }

    private void OnContinueButtonClicked()
    {
        GameEventBus.Publish<ShopContinueClickedEvent>();
    }

    private void DisplayProperties()
    {
        ShowPanel(propertiesSidebar);
    }

    private void DisplayInventory()
    {
        ShowPanel(inventorySidebar);
    }

    private void OnCloseSidebarClicked()
    {
        HideCurrentSidebarPanel();
    }

    private static void BindClick(UIClickTarget clickTarget, System.Action handler)
    {
        clickTarget.OnClicked += handler;
    }

    private static void UnbindClick(UIClickTarget clickTarget, System.Action handler)
    {
        clickTarget.OnClicked -= handler;
    }

    private void InitSidebarPanels()
    {
        RefreshSidebarDefaults(propertiesSidebar);
        RefreshSidebarDefaults(inventorySidebar);
    }

    private void ShowPanel(UISidebarRevealMotion panel)
    {

        if (currentOpenPanel != null && currentOpenPanel != panel)
        {
            currentOpenPanel.Hide();
        }

        panel.Show();
        currentOpenPanel = panel;

        closeSidebarButton.gameObject.SetActive(true);
    }

    private void HideCurrentSidebarPanel()
    {
        if (currentOpenPanel != null)
        {
            currentOpenPanel.Hide();
            currentOpenPanel = null;
        }

        closeSidebarButton.gameObject.SetActive(false);
    }

    private void HideAllSidebarPanelsImmediately()
    {
        HideSidebarImmediate(propertiesSidebar);
        HideSidebarImmediate(inventorySidebar);
        currentOpenPanel = null;

        closeSidebarButton.gameObject.SetActive(false);
    }

    private void KillPanelTweens()
    {
        KillSidebarTween(propertiesSidebar);
        KillSidebarTween(inventorySidebar);
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
        foreach (var item in spawnedItems)
        {
                item.CleanUp();
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void InjectPropertiesDependencies()
    {
        Player player = FindFirstObjectByType<Player>();

        PropertiesManager manager = player != null ? player.GetComponent<PropertiesManager>() : null;
        propertiesViewSync.InjectDependencies(manager);
    }

    private static void RefreshSidebarDefaults(UISidebarRevealMotion sidebar)
    {
        if (sidebar == null)
        {
            return;
        }

        sidebar.RefreshDefaults();
    }

    private static void HideSidebarImmediate(UISidebarRevealMotion sidebar)
    {
        sidebar.SetExitImmediate();
    }

    private static void KillSidebarTween(UISidebarRevealMotion sidebar)
    {
        sidebar.Kill();
    }
}
