using System;
using System.Collections.Generic;
using Orange.UIFramework;
using UnityEngine;

public class ShopItemListUI : ViewPartBase
{
    [SerializeField] private ShopItemContainer itemPrefab;
    [SerializeField] private Transform itemParent;

    private readonly List<ShopItemContainer> renderedItems = new();
    private readonly List<ShopItemIdentity> renderedItemIdentities = new();
    private UIManager uiManager;

    public event Action<int> BuyRequested;
    public event Action<int> LockToggleRequested;

    private void Awake()
    {
        ValidateConfiguration();
        itemParent.Clear();
    }

    private void OnDestroy()
    {
        Clear();
        BuyRequested = null;
        LockToggleRequested = null;
    }

    public void Render(ShopItemData[] items, ShopRefreshReason reason)
    {
        if (items == null || items.Length == 0)
        {
            Clear();
            return;
        }

        List<ShopItemContainer> previousItems = new(renderedItems);
        List<ShopItemIdentity> previousIdentities = new(renderedItemIdentities);
        bool[] previousItemConsumed = new bool[previousItems.Count];

        renderedItems.Clear();
        renderedItemIdentities.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            RenderItem(
                items[i],
                i,
                previousItems,
                previousIdentities,
                previousItemConsumed);
        }

        DestroyUnusedPreviousItems(previousItems, previousItemConsumed);
    }

    public void Clear()
    {
        for (int i = 0; i < renderedItems.Count; i++)
        {
            DestroyItem(renderedItems[i]);
        }

        renderedItems.Clear();
        renderedItemIdentities.Clear();
    }

    private void RenderItem(
        ShopItemData itemData,
        int itemIndex,
        List<ShopItemContainer> previousItems,
        List<ShopItemIdentity> previousIdentities,
        bool[] previousItemConsumed)
    {
        if (itemData.ItemData == null)
        {
            Debug.LogWarning($"{nameof(ShopItemListUI)} on '{name}' skipped rendering a shop item without {nameof(ItemDataSO)}.", this);
            return;
        }

        ShopItemIdentity nextIdentity = ShopItemIdentity.From(itemData);
        int reusableItemIndex = FindReusableItemIndex(nextIdentity, previousItems, previousIdentities, previousItemConsumed);
        ShopItemContainer container = reusableItemIndex >= 0
            ? previousItems[reusableItemIndex]
            : CreateItem();

        if (reusableItemIndex >= 0)
        {
            previousItemConsumed[reusableItemIndex] = true;
        }

        container.transform.SetSiblingIndex(itemIndex);
        container.Configure(new InfoAddIndex<ShopItemData>(itemData, itemIndex));
        ConfigureTooltip(container);

        renderedItems.Add(container);
        renderedItemIdentities.Add(nextIdentity);
    }

    private ShopItemContainer CreateItem()
    {
        ShopItemContainer container = Instantiate(itemPrefab, itemParent);
        BindItemCallbacks(container);
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

            DestroyItem(previousItems[i]);
        }
    }

    private void DestroyItem(ShopItemContainer item)
    {
        if (item == null)
        {
            return;
        }

        UnbindItemCallbacks(item);
        item.CleanUp();
        Destroy(item.gameObject);
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

    private void BindItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested += OnItemBuyRequested;
        container.LockToggleRequested += OnItemLockToggleRequested;
    }

    public void ConfigureOwner(UIManager ownerUIManager)
    {
        uiManager = ownerUIManager;
    }

    private void ConfigureTooltip(ShopItemContainer container)
    {
        TooltipTrigger tooltipTrigger = container.GetComponent<TooltipTrigger>();
        if (tooltipTrigger != null)
        {
            tooltipTrigger.Configure(container, uiManager, canPin: true, interactiveTransient: true);
        }
    }

    private void UnbindItemCallbacks(ShopItemContainer container)
    {
        container.BuyRequested -= OnItemBuyRequested;
        container.LockToggleRequested -= OnItemLockToggleRequested;
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        BuyRequested?.Invoke(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        LockToggleRequested?.Invoke(itemIndex);
    }

    private void ValidateConfiguration()
    {
        if (itemPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(ShopItemListUI)} '{name}' is missing shop item prefab.");
        }

        if (itemParent == null)
        {
            throw new MissingReferenceException($"{nameof(ShopItemListUI)} '{name}' is missing item parent.");
        }
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
}
