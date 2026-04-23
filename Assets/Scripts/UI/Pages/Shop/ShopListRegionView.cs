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
        ClearShopItems();
        if (items == null || items.Length == 0)
        {
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            SpawnShopItem(items[i]);
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

    private void SpawnShopItem(ShopItemData itemData)
    {
        ShopItemContainer container = UnityEngine.Object.Instantiate(shopItemPrefab, shopItemParent);
        if (itemData.ItemData == null)
        {
            Debug.LogWarning($"{nameof(ShopListRegionView)} on '{ownerName}' skipped rendering a shop item without {nameof(ItemDataSO)}.");
            UnityEngine.Object.Destroy(container.gameObject);
            return;
        }

        container.Configure(new InfoAddIndex<ShopItemData>(itemData, spawnedItems.Count));
        BindShopItemCallbacks(container);
        spawnedItems.Add(container);
    }

    private void ClearShopItems()
    {
        foreach (ShopItemContainer item in spawnedItems)
        {
            UnbindShopItemCallbacks(item);
            item.CleanUp();
            UnityEngine.Object.Destroy(item.gameObject);
        }

        spawnedItems.Clear();
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
}
