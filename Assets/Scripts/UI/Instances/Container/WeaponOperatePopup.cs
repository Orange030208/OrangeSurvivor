using TMPro;
using UnityEngine;

public class WeaponOperatePopup : UIContainerBase<InventoryItemOperateResource, ExtraInfoDescriber>
{
    [SerializeField] private UIClickTarget sellButton;
    [SerializeField] private UIClickTarget mergeButton;
    [SerializeField] private TextMeshProUGUI sellPriceText;

    private int currentItemIndex = -1;

    public override void Configure(InventoryItemOperateResource resource)
    {
        nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.itemData.ItemName, resource.colorDependencyNumber);
        iconImage.sprite = resource.itemData.Icon;
        sellPriceText.text = resource.sellPrice.ToString();

        RenderColor(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.itemData);

        currentItemIndex = resource.itemIndex;

        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;

        bool canMerge = WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);

        sellButton.OnClicked += OnSellClicked;
        if (canMerge)
        {
            mergeButton.OnClicked += OnMergeClicked;
        }

        CleanClickEvent();
    }

    public override void Dispose()
    {
        base.Dispose();
        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;
        currentItemIndex = -1;
    }

    private void OnSellClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new InventoryItemSellClickedEvent(currentItemIndex));
    }

    private void OnMergeClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new InventoryItemMergeClickedEvent(currentItemIndex));
    }
}
