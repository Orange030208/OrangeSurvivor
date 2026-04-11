using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : UIContainerBase<InfoAddIndex<ShopItemData>, CharacterExtraInfoDisplayer>
{
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;

    public override void Configure(InfoAddIndex<ShopItemData> resource)
    {
        ShopItemData shopItem = resource.info;
        ItemDataSO itemData = shopItem.ItemData;

        if (itemData == null)
        {
            Debug.LogWarning("ItemDataSO is null in ShopItemContainer.Configure");
            return;
        }

        int colorDependency;
        List<string> descriptions;

        if (itemData is AccessoryDataSO accessoryData)
        {
            colorDependency = accessoryData.Rarity;
            descriptions = accessoryData.GetAutoDescriptions();
            nameText.text = itemData.ItemName;
            bottom.DisplayDescriptions(descriptions);
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            descriptions = weaponData.GetAutoDescriptions(shopItem.Level);
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level);
            bottom.DisplayDescriptions(descriptions);
        }
        else
        {
            Debug.LogWarning($"Unsupported ItemDataSO type: {itemData.GetType().Name}");
            return;
        }

        lockImage.sprite = shopItem.Lock ? lockSprite : unlockSprite;

        priceText.text = shopItem.GetPrice().ToString();
        RenderColor(itemData, colorDependency);

        CleanClickEvent();

        buyButton.onClick.RemoveAllListeners();

        lockButton.onClick.RemoveAllListeners();
        lockButton.onClick.AddListener(() =>
        {
            GameEventBus.Publish(new OperateShopItemLockEvent(resource.index));
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        buyButton.onClick.RemoveAllListeners();
        lockButton.onClick.RemoveAllListeners();
    }
}
