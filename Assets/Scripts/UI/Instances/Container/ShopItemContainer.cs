using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : UIContainerBase<InfoAddIndex<ShopItemData>, UIPropertiesViewList>
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
        Dictionary<PropType, float> props;

        if (itemData is AccessoryDataSO accessoryData)
        {
            colorDependency = accessoryData.Rarity;
            props = accessoryData.GetProps();
            nameText.text = itemData.ItemName;
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            colorDependency = shopItem.Level;
            props = weaponData.GetPropsByLevel(shopItem.Level);
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, shopItem.Level);
        }
        else
        {
            Debug.LogWarning($"Unsupported ItemDataSO type: {itemData.GetType().Name}");
            return;
        }

        lockImage.sprite = shopItem.Lock ? lockSprite : unlockSprite;

        priceText.text = shopItem.GetPrice().ToString();
        RenderColor(itemData, colorDependency);
        bottom.Render(ToPropEntries(props));

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

    private List<PropEntry> ToPropEntries(Dictionary<PropType, float> props)
    {
        List<PropEntry> entries = new();
        foreach (var kv in props)
        {
            entries.Add(new PropEntry(kv.Key, kv.Value));
        }

        return entries;
    }
}
