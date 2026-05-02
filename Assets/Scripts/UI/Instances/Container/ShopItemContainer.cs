using AXR.Framework.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : UIContainerBase<InfoAddIndex<ShopItemData>, ExtraInfoDescriber>
{
    [SerializeField] private UIClickTarget buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private UIClickTarget lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;
    [SerializeField] private CardMotionController cardMotionController;

    private int currentIndex = -1;

    public event Action<int> BuyRequested;
    public event Action<int> LockToggleRequested;

    public override void Configure(InfoAddIndex<ShopItemData> resource)
    {
        Configure(resource, true);
    }

    public void Configure(InfoAddIndex<ShopItemData> resource, bool playReveal)
    {
        Configure(resource, playReveal, refreshMotion: true);
    }

    public void Configure(InfoAddIndex<ShopItemData> resource, bool playReveal, bool refreshMotion)
    {
        ShopItemData shopItem = resource.info;
        ItemDataSO itemData = shopItem.ItemData;

        if (itemData == null)
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' received a shop item without {nameof(ItemDataSO)}.");
        }

        int colorDependency;
        if (itemData is AccessoryDataSO accessoryData)
        {
            colorDependency = accessoryData.Rarity;
            nameText.text = itemData.ItemName;
            bottom.Display(itemData);
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level);
            bottom.Display(weaponData);
        }
        else
        {
            throw new InvalidOperationException($"{nameof(ShopItemContainer)} '{name}' does not support item type '{itemData.GetType().Name}'.");
        }

        lockImage.sprite = shopItem.Lock ? lockSprite : unlockSprite;

        priceText.text = shopItem.GetPrice().ToString();
        iconImage.sprite = itemData.ItemIcon;
        RenderItemQuality(itemData, colorDependency);

        CleanClickEvent();

        buyButton.OnClicked -= OnBuyButtonClicked;
        lockButton.OnClicked -= OnLockButtonClicked;

        currentIndex = resource.index;

        if (refreshMotion)
        {
            ConfigureCardMotionForReuse(playReveal);
        }

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
        BuyRequested = null;
        LockToggleRequested = null;
        currentIndex = -1;
    }

    private void OnBuyButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        BuyRequested?.Invoke(currentIndex);
    }

    private void OnLockButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        LockToggleRequested?.Invoke(currentIndex);
    }

    private void ConfigureCardMotionForReuse(bool playReveal)
    {
        if (cardMotionController == null)
        {
            cardMotionController = GetComponent<CardMotionController>();
        }

        cardMotionController?.ConfigureForReuse(playReveal);
    }
}
