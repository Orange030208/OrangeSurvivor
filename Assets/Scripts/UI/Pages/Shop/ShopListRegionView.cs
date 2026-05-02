using System;
using AXR.Framework.UI;
using TMPro;
using UnityEngine;

public sealed class ShopListRegionView
{
    private readonly string ownerName;
    private readonly ShopItemGroupView shopItemGroup;
    private readonly UIClickTarget rerollButton;
    private readonly UIClickTarget continueButton;
    private readonly TextMeshProUGUI rerollCostText;
    private readonly TextMeshProUGUI currencyText;

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
        shopItemGroup = new ShopItemGroupView(this.ownerName, shopItemPrefab, shopItemParent);
        this.rerollButton = rerollButton ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing reroll button.");
        this.continueButton = continueButton ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing continue button.");
        this.rerollCostText = rerollCostText ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing reroll cost text.");
        this.currencyText = currencyText ?? throw new MissingReferenceException($"{nameof(ShopUIPage)} '{this.ownerName}' is missing currency text.");

        shopItemGroup.ItemBuyRequested += OnItemBuyRequested;
        shopItemGroup.ItemLockToggleRequested += OnItemLockToggleRequested;
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

        shopItemGroup.Clear();
    }

    public void RenderShopItems(ShopItemData[] items, ShopSnapshotReason reason)
    {
        shopItemGroup.Render(items, reason);
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
