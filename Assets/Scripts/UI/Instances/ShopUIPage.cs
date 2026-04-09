using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUIPage : UIPageBase
{

    [SerializeField] private ShopItemContainer shopItemPrefab;
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private Button showPropertiesButton;
    [SerializeField] private Button showInventoryButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button watchVideoRerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;

    [Header("属性面板(左)")]
    [SerializeField] private SidebarSlider propertiesSidebar;
    [SerializeField] private UIPropertiesViewSync propertiesViewSync;

    [Header("背包面板(右)")]
    [SerializeField] private SidebarSlider inventorySidebar;

    [Header("侧边遮罩")]
    [SerializeField] private ClickOnlyHandler closeSidebarPanel;

    private readonly List<ShopItemContainer> spawnedItems = new();
    private int currentRerollCost;

    private SidebarSlider currentOpenPanel;

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
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        BindButtonEvents();
        InjectPropertiesDependencies();
        propertiesViewSync?.StartSync();
        HideAllSidebarPanelsImmediately();
        GameEventBus.Publish(new RequestShopSnapshotEvent());
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Unsubscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Unsubscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        UnbindButtonEvents();
        propertiesViewSync?.StopSync();
        KillPanelTweens();
        ClearShopItems();
    }

    private void BindButtonEvents()
    {
        rerollButton?.onClick.AddListener(OnRerollButtonClicked);
        watchVideoRerollButton?.onClick.AddListener(OnWatchVideoRerollButtonClicked);
        showPropertiesButton?.onClick.AddListener(DisplayProperties);
        showInventoryButton?.onClick.AddListener(DisplayInventory);

        if (closeSidebarPanel != null)
        {
            closeSidebarPanel.OnClick += OnCloseSidebarClicked;
        }
    }

    private void UnbindButtonEvents()
    {
        rerollButton?.onClick.RemoveListener(OnRerollButtonClicked);
        watchVideoRerollButton?.onClick.RemoveListener(OnWatchVideoRerollButtonClicked);
        showPropertiesButton?.onClick.RemoveListener(DisplayProperties);
        showInventoryButton?.onClick.RemoveListener(DisplayInventory);

        if (closeSidebarPanel != null)
        {
            closeSidebarPanel.OnClick -= OnCloseSidebarClicked;
            closeSidebarPanel.Dispose();
        }
    }

    private void OnShopItemsChanged(ShopItemsChangedEvent eventData)
    {
        ClearShopItems();
        currentRerollCost = eventData.RerollCost;
        UpdateRerollCostDisplay();

        if (eventData.Items == null || eventData.Items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < eventData.Items.Length; i++)
        {
            SpawnShopItem(eventData.Items[i]);
        }
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

    private void OnShopItemClicked(ShopItemContainer container)
    {
        int index = spawnedItems.IndexOf(container);
        if (index >= 0)
        {
            GameEventBus.Publish(new ShopItemClickedEvent(index));
        }
    }

    private void OnShopItemLockClicked(ShopItemContainer container)
    {
        int index = spawnedItems.IndexOf(container);
        if (index >= 0)
        {
            GameEventBus.Publish(new OperateShopItemLockEvent(index));
        }
    }

    private void OnRerollButtonClicked()
    {
        GameEventBus.Publish(new ShopRerollRequestedEvent());
    }

    private void OnWatchVideoRerollButtonClicked()
    {
        GameEventBus.Publish(new ShopVideoAdRerollRequestedEvent());
    }

    private void DisplayProperties()
    {
        ShowPanel(propertiesSidebar);
    }

    private void DisplayInventory()
    {
        ShowPanel(inventorySidebar);
    }

    private void OnCloseSidebarClicked(PointerEventData _)
    {
        HideCurrentSidebarPanel();
    }

    private void UnDisplayProperties()
    {
        HideCurrentSidebarPanel();
    }

    private void InitSidebarPanels()
    {
        propertiesSidebar?.CachePositionsByCurrentState();
        inventorySidebar?.CachePositionsByCurrentState();
    }

    private void ShowPanel(SidebarSlider panel)
    {
        if (panel == null)
        {
            return;
        }

        if (currentOpenPanel != null && currentOpenPanel != panel)
        {
            currentOpenPanel.Hide();
        }

        panel.Show();
        currentOpenPanel = panel;

        if (closeSidebarPanel != null)
        {
            closeSidebarPanel.gameObject.SetActive(true);
        }
    }

    private void HideCurrentSidebarPanel()
    {
        if (currentOpenPanel != null)
        {
            currentOpenPanel.Hide();
            currentOpenPanel = null;
        }

        if (closeSidebarPanel != null)
        {
            closeSidebarPanel.gameObject.SetActive(false);
        }
    }

    private void HideAllSidebarPanelsImmediately()
    {
        propertiesSidebar?.HideImmediate();
        inventorySidebar?.HideImmediate();
        currentOpenPanel = null;

        if (closeSidebarPanel != null)
        {
            closeSidebarPanel.gameObject.SetActive(false);
        }
    }

    private void KillPanelTweens()
    {
        propertiesSidebar?.KillTween();
        inventorySidebar?.KillTween();
    }

    private void OnPurchaseSuccess(ShopPurchaseSuccessEvent eventData)
    {
        Debug.Log($"Purchase successful: {eventData.ItemData.ItemType}");
    }

    private void OnPurchaseFailed(ShopPurchaseFailedEvent eventData)
    {
        Debug.LogWarning($"Purchase failed: {eventData.Message}");
    }

    private void OnCurrencyChanged(CurrencyChangedEvent eventData)
    {
        UpdateRerollButtonInteractable();
    }

    private void UpdateRerollCostDisplay()
    {
        if (rerollCostText != null)
        {
            rerollCostText.text = currentRerollCost.ToString();
        }

        UpdateRerollButtonInteractable();
    }

    private void UpdateRerollButtonInteractable()
    {
        if (rerollButton != null)
        {
            rerollButton.interactable = CurrencyManager.Instance.Currency >= currentRerollCost;
        }
    }

    private void ClearShopItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                item.CleanUp();
                Destroy(item.gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void InjectPropertiesDependencies()
    {
        if (propertiesViewSync == null)
        {
            return;
        }

        Player player  = FindFirstObjectByType<Player>();

        PropertiesManager manager = player != null ? player.GetComponent<PropertiesManager>() : null;
        propertiesViewSync.InjectDependencies(manager);
    }
}
