using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : UIContainerBase<InfoAddIndex<ShopItemData>, DescriptionListDisplayer>
{
    [SerializeField] private UIClickTarget buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private UIClickTarget lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;

    private int currentIndex = -1;

    public override void Configure(InfoAddIndex<ShopItemData> resource)
    {
        ShopItemData shopItem = resource.info;
        ItemDataSO itemData = shopItem.ItemData;

        if (itemData == null)
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' received a shop item without {nameof(ItemDataSO)}.");
        }

        int colorDependency;
        IReadOnlyList<string> descriptions;

        if (itemData is AccessoryDataSO accessoryData)
        {
            colorDependency = accessoryData.Rarity;
            descriptions = accessoryData.GetDescriptions();
            nameText.text = itemData.ItemName;
            bottom.DisplayDescriptions(descriptions);
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            descriptions = weaponData.GetDescriptions(shopItem.Level);
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level);
            bottom.DisplayDescriptions(descriptions);
        }
        else
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' does not support item type '{itemData.GetType().Name}'.");
        }

        lockImage.sprite = shopItem.Lock ? lockSprite : unlockSprite;

        priceText.text = shopItem.GetPrice().ToString();
        RenderColor(itemData, colorDependency);

        CleanClickEvent();

        buyButton.OnClicked -= OnBuyButtonClicked;
        lockButton.OnClicked -= OnLockButtonClicked;

        currentIndex = resource.index;

        buyButton.OnClicked += OnBuyButtonClicked;
        lockButton.OnClicked += OnLockButtonClicked;
    }

    public override void Dispose()
    {
        base.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        buyButton.OnClicked -= OnBuyButtonClicked;
        lockButton.OnClicked -= OnLockButtonClicked;
        currentIndex = -1;
    }

    private void OnBuyButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new ShopItemClickedEvent(currentIndex));
    }

    private void OnLockButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new OperateShopItemLockEvent(currentIndex));
    }
}
