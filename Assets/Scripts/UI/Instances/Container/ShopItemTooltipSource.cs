using Orange.UIFramework;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopItemTooltipSource : MonoBehaviour, ITooltipContentSource
{
    [SerializeField] private ShopItemContainer targetContainer;

    private readonly ItemInfoViewDataBuilder itemInfoBuilder = new();
    private ShopItemData currentShopItem;

    public void Bind(ShopItemData shopItem)
    {
        currentShopItem = shopItem;
    }

    public void Clear()
    {
        currentShopItem = default;
    }

    public bool TryBuildTooltipContent(out TooltipContent content)
    {
        ItemDataSO itemData = currentShopItem.ItemData;
        ItemInfoViewData data;
        if (itemData is WeaponDataSO weaponData)
        {
            data = itemInfoBuilder.Build(weaponData, currentShopItem.Level);
        }
        else if (itemData is AccessoryDataSO accessoryData)
        {
            data = itemInfoBuilder.Build(accessoryData);
        }
        else
        {
            data = itemInfoBuilder.Build(itemData);
        }

        if (string.IsNullOrWhiteSpace(data.Name) && string.IsNullOrWhiteSpace(data.BodyRichText))
        {
            content = null;
            return false;
        }

        content = new TooltipContent(
            ItemInfoTooltipView.VIEW_ID,
            data,
            new TooltipChromeOptions(
                allowUserPin: false,
                showCloseButton: false,
                allowInteractiveTransient: true));
        return true;
    }

    private void Awake()
    {
        if (targetContainer == null)
        {
            targetContainer = GetComponent<ShopItemContainer>();
        }
    }
}
