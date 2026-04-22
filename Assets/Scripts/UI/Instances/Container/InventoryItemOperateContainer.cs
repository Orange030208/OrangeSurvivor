using TMPro;
using UnityEngine;

public class InventoryItemOperateContainer : UIContainerBase<InventoryItemOperateResource, ExtraInfoDescriber>
{
    [SerializeField] private UIClickTarget sellButton;
    [SerializeField] private UIClickTarget mergeButton;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private GameObject sellSection;
    [SerializeField] private GameObject mergeSection;

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

        if (sellPriceText != null)
        {
            sellPriceText.text = resource.sellPrice.ToString();
        }

        RenderColor(resource.itemData, resource.colorDependencyNumber);
        bottom.Display(resource.itemData);

        currentItemIndex = resource.itemIndex;

        sellButton.OnClicked -= OnSellClicked;
        mergeButton.OnClicked -= OnMergeClicked;

        bool isWeapon = resource.itemData.ItemType == ItemType.Weapon;
        bool canMerge = isWeapon && WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);

        if (sellSection != null)
        {
            sellSection.SetActive(isWeapon);
        }
        else
        {
            sellButton.gameObject.SetActive(isWeapon);
        }

        if (mergeSection != null)
        {
            mergeSection.SetActive(canMerge);
        }
        else
        {
            mergeButton.gameObject.SetActive(canMerge);
        }

        if (isWeapon)
        {
            sellButton.OnClicked += OnSellClicked;
        }

        if (canMerge)
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
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new InventoryItemSellClickedEvent(currentItemIndex));
    }

    private void OnMergeClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        GameEventBus.Publish(new InventoryItemMergeClickedEvent(currentItemIndex));
    }
}

public readonly struct InventoryItemOperateResource
{
    public readonly int itemIndex;
    public readonly ItemDataSO itemData;
    public readonly int colorDependencyNumber;
    public readonly int sellPrice;
    public readonly IDescribable describable;

    public InventoryItemOperateResource(
        int itemIndex,
        ItemDataSO itemData,
        int colorDependencyNumber,
        int sellPrice,
        IDescribable describable)
    {
        this.itemIndex = itemIndex;
        this.itemData = itemData;
        this.colorDependencyNumber = colorDependencyNumber;
        this.sellPrice = sellPrice;
        this.describable = describable;
    }
}
