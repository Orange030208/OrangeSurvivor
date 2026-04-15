using TMPro;
using UnityEngine;

public class InventoryItemOperateContainer : UIContainerBase<InventoryItemOperateResource, DescriptionListDisplayer>
{
    [SerializeField] private UIClickTarget sellButton;
    [SerializeField] private UIClickTarget mergeButton;
    [SerializeField] private TextMeshProUGUI sellPriceText;

    private int currentItemIndex = -1;

    public override void Configure(InventoryItemOperateResource resource)
    {
        if (resource.itemData == null)
        {
            return;
        }

        if (resource.itemData.ItemType == ItemType.Weapon)
        {
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(resource.itemData.ItemName, resource.colorDependencyNumber);
        }
        else
        {
            nameText.text = resource.itemData.ItemName;
        }

        sellPriceText.text = resource.sellPrice.ToString();

        RenderColor(resource.itemData, resource.colorDependencyNumber);
        bottom.DisplayDescriptions(resource.descriptions);

        currentItemIndex = resource.itemIndex;

        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;

        sellButton.OnClicked += OnSellClicked;

        bool showMerge = resource.itemData.ItemType == ItemType.Weapon && WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);
        mergeButton.gameObject.SetActive(showMerge);
        if (showMerge)
        {
            mergeButton.OnClicked += OnMergeClicked;
        }

        CleanClickEvent();
        OnClicked += _ =>
        {
            GameEventBus.Publish(new InventoryItemOperatePanelCloseClickedEvent(resource.itemIndex));
        };
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
        GameEventBus.Publish(new InventoryItemSellClickedEvent(currentItemIndex));
    }

    private void OnMergeClicked()
    {
        GameEventBus.Publish(new InventoryItemMergeClickedEvent(currentItemIndex));
    }
}

public readonly struct InventoryItemOperateResource
{
    public readonly int itemIndex;
    public readonly ItemDataSO itemData;
    public readonly int colorDependencyNumber;
    public readonly int sellPrice;
    public readonly System.Collections.Generic.IReadOnlyList<string> descriptions;

    public InventoryItemOperateResource(
        int itemIndex,
        ItemDataSO itemData,
        int colorDependencyNumber,
        int sellPrice,
        System.Collections.Generic.IReadOnlyList<string> descriptions)
    {
        this.itemIndex = itemIndex;
        this.itemData = itemData;
        this.colorDependencyNumber = colorDependencyNumber;
        this.sellPrice = sellPrice;
        this.descriptions = descriptions;
    }
}
