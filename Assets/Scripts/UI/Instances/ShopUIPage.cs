using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIPage : UIPageBase
{
    [SerializeField] private ShopItemContainer shopItemPrefab;
    [SerializeField] private Transform shopItemParent;
    [SerializeField] private Button showPropButton;
    [SerializeField] private Button showInventoryButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button watchVideoRerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;

    private List<ShopItemContainer> spawnedItems = new();
    private int currentRerollCost;

    protected override void Awake()
    {
        base.Awake();
        shopItemParent.Clear();
    }

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Subscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Subscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        BindButtonEvents();
        GameEventBus.Publish(new RequestShopSnapshotEvent());
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<ShopItemsChangedEvent>(OnShopItemsChanged);
        GameEventBus.Unsubscribe<ShopPurchaseSuccessEvent>(OnPurchaseSuccess);
        GameEventBus.Unsubscribe<ShopPurchaseFailedEvent>(OnPurchaseFailed);
        GameEventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);

        UnbindButtonEvents();
        ClearShopItems();
    }

    private void BindButtonEvents()
    {
        rerollButton?.onClick.AddListener(OnRerollButtonClicked);
        watchVideoRerollButton?.onClick.AddListener(OnWatchVideoRerollButtonClicked);
    }

    private void UnbindButtonEvents()
    {
        rerollButton?.onClick.RemoveListener(OnRerollButtonClicked);
        watchVideoRerollButton?.onClick.RemoveListener(OnWatchVideoRerollButtonClicked);
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
        container.OnLockClicked += () => OnShopItemLockClicked(container);
        container.OnItemClicked += () => OnShopItemClicked(container);

        if (itemData.ItemData != null)
        {
            container.Configure(itemData.ItemData, itemData.Lock, itemData.Level);
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
}
