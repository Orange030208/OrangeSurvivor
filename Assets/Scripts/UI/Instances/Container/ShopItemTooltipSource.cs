using Orange.UIFramework;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopItemTooltipSource : MonoBehaviour, ITooltipContentSource
{
    [SerializeField] private ShopItemContainer targetContainer;

    private readonly ItemInfoViewDataBuilder itemInfoBuilder = new();
    private ShopOfferViewData currentShopOffer;

    public void Bind(ShopOfferViewData shopOffer)
    {
        currentShopOffer = shopOffer;
    }

    public void Clear()
    {
        currentShopOffer = default;
    }

    public bool TryBuildTooltipContent(out TooltipContent content)
    {
        ItemInfoViewData data = currentShopOffer.InfoDocument != null
            ? itemInfoBuilder.Build(
                currentShopOffer.InfoDocument,
                currentShopOffer.TypeText,
                currentShopOffer.DisplayName)
            : itemInfoBuilder.Build(currentShopOffer.DisplayItem);

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
