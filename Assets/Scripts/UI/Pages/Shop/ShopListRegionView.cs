using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ShopListRegionView
{
    private readonly string ownerName;
    private readonly ShopItemContainer shopItemPrefab;
    private readonly Transform shopItemParent;
    private readonly UIClickTarget rerollButton;
    private readonly UIClickTarget continueButton;
    private readonly TextMeshProUGUI rerollCostText;
    private readonly TextMeshProUGUI currencyText;
    private readonly List<ShopItemContainer> spawnedItems = new();
    private readonly List<ShopItemIdentity?> renderedItemIdentities = new();

    private bool bound;

    public ShopListRegionView(
        string ownerName,
        ShopItemContainer shopItemPrefab,
        Transform shopItemParent,
        UIClickTarget rerollButton,
        UIClickTarget continueButton,
        TextMeshProUGUI rerollCostText,
        TextMeshProUGUI currencyText)
    {
        this.ownerName = string.IsNullOrWhiteSpace(ownerName) ? nameof(ShopListRegionView) : ownerName;
        this.shopItemPrefab = shopItemPrefab ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing shop item prefab.");
        this.shopItemParent = shopItemParent ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing shop item parent.");
        this.rerollButton = rerollButton ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing reroll button.");
        this.continueButton = continueButton ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing continue button.");
        this.rerollCostText = rerollCostText ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing reroll cost text.");
        this.currencyText = currencyText ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing currency text.");

        shopItemParent.Clear();
    }

    public event Action RerollRequested;
    public event Action ContinueRequested;
    public event Action<int> ItemBuyRequested;
    public event Action<int> ItemLockToggleRequested;

    public void Bind()
    {
        if (bound)
        {
            return;
        }

        rerollButton.OnClicked += OnRerollButtonClicked;
        continueButton.OnClicked += OnContinueButtonClicked;
        bound = true;
    }

    public void Unbind()
    {
        if (bound)
        {
            rerollButton.OnClicked -= OnRerollButtonClicked;
            continueButton.OnClicked -= OnContinueButtonClicked;
            bound = false;
        }

        ClearShopItems();
    }

    public void RenderShopItems(ShopItemData[] items)
    {
        if (items == null || items.Length == 0)
        {
            ClearShopItems();
            return;
        }

        TrimExtraItems(items.Length);
        for (int i = 0; i < items.Length; i++)
        {
            RenderShopItem(items[i], i);
        }
    }

    public void UpdateRerollState(int rerollCost, bool canReroll)
    {
        rerollCostText.text = rerollCost.ToString();
        rerollButton.Interactable = canReroll;
    }

    public void UpdateCurrencyAmount(int amount)
    {
        currencyText.text = amount.ToString();
    }

    private void RenderShopItem(ShopItemData itemData, int itemIndex)
    {
        if (itemData.ItemData == null)
        {
            Debug.LogWarning($"{nameof(ShopListRegionView)} on '{ownerName}' skipped rendering a shop item without {nameof(ItemDataSO)}.");
            ClearShopItemSlot(itemIndex);
            return;
        }

        ShopItemContainer container = GetOrCreateShopItem(itemIndex);
        ShopItemIdentity nextIdentity = ShopItemIdentity.From(itemData);
        EnsureIdentitySlotCount(itemIndex + 1);
        bool playReveal = !renderedItemIdentities[itemIndex].HasValue
            || !renderedItemIdentities[itemIndex].Value.Equals(nextIdentity);
        container.Configure(new InfoAddIndex<ShopItemData>(itemData, itemIndex), playReveal);

        renderedItemIdentities[itemIndex] = nextIdentity;
    }

    private void ClearShopItems()
    {
        foreach (ShopItemContainer item in spawnedItems)
        {
            if (item == null)
            {
                continue;
            }

            UnbindShopItemCallbacks(item);
            item.CleanUp();
            UnityEngine.Object.Destroy(item.gameObject);
        }

        spawnedItems.Clear();
        renderedItemIdentities.Clear();
    }

    private ShopItemContainer GetOrCreateShopItem(int itemIndex)
    {
        EnsureItemSlotCount(itemIndex + 1);
        if (spawnedItems[itemIndex] != null)
        {
            return spawnedItems[itemIndex];
        }

        ShopItemContainer container = UnityEngine.Object.Instantiate(shopItemPrefab, shopItemParent);
        BindShopItemCallbacks(container);
        spawnedItems[itemIndex] = container;
        return container;
    }

    private void TrimExtraItems(int itemCount)
    {
        for (int i = spawnedItems.Count - 1; i >= itemCount; i--)
        {
            ClearShopItemSlot(i);
            spawnedItems.RemoveAt(i);
        }

        if (renderedItemIdentities.Count > itemCount)
        {
            renderedItemIdentities.RemoveRange(itemCount, renderedItemIdentities.Count - itemCount);
        }
    }

    private void ClearShopItemSlot(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= spawnedItems.Count)
        {
            if (itemIndex >= 0 && itemIndex < renderedItemIdentities.Count)
            {
                renderedItemIdentities[itemIndex] = null;
            }

            return;
        }

        ShopItemContainer item = spawnedItems[itemIndex];
        if (item != null)
        {
            UnbindShopItemCallbacks(item);
            item.CleanUp();
            UnityEngine.Object.Destroy(item.gameObject);
            spawnedItems[itemIndex] = null;
        }

        if (itemIndex < renderedItemIdentities.Count)
        {
            renderedItemIdentities[itemIndex] = null;
        }
    }

    private void EnsureItemSlotCount(int itemCount)
    {
        while (spawnedItems.Count < itemCount)
        {
            spawnedItems.Add(null);
        }
    }

    private void EnsureIdentitySlotCount(int itemCount)
    {
        while (renderedItemIdentities.Count < itemCount)
        {
            renderedItemIdentities.Add(null);
        }
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

    private void OnRerollButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        RerollRequested?.Invoke();
    }

    private void OnContinueButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        ContinueRequested?.Invoke();
    }

    private void OnItemBuyRequested(int itemIndex)
    {
        ItemBuyRequested?.Invoke(itemIndex);
    }

    private void OnItemLockToggleRequested(int itemIndex)
    {
        ItemLockToggleRequested?.Invoke(itemIndex);
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
