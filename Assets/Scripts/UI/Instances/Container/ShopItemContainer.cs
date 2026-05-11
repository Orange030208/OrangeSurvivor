using Orange.UIFramework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : UIContainerBase<InfoAddIndex<ShopItemData>, ExtraInfoDescriber>
{
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button lockButton;
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
            bottom.Display(accessoryData);
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level);
            bottom.Display(new WeaponLevelDescribable(weaponData, shopItem.Level));
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

        buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        lockButton.onClick.RemoveListener(OnLockButtonClicked);

        currentIndex = resource.index;

        if (refreshMotion)
        {
            ConfigureCardMotionForReuse(playReveal);
        }

        buyButton.onClick.AddListener(OnBuyButtonClicked);
        lockButton.onClick.AddListener(OnLockButtonClicked);
    }

    public override void Dispose()
    {
        base.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        lockButton.onClick.RemoveListener(OnLockButtonClicked);
        BuyRequested = null;
        LockToggleRequested = null;
        currentIndex = -1;
    }

    private void OnBuyButtonClicked()
    {
        BuyRequested?.Invoke(currentIndex);
    }

    private void OnLockButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.UiConfirm);
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
