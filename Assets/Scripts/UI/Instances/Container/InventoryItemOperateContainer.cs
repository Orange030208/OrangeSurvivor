using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemOperateContainer : UIContainerBase<InventoryItemOperateResource, UIPropertiesViewList>
{
    [SerializeField] private Button sellButton;
    [SerializeField] private Button mergeButton;
    [SerializeField] private TextMeshProUGUI sellPriceText;

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
        bottom.Render(ToPropEntries(resource.propDictionary));

        sellButton.onClick.RemoveAllListeners();
        mergeButton.onClick.RemoveAllListeners();

        sellButton.onClick.AddListener(() =>
        {
            GameEventBus.Publish(new InventoryItemSellClickedEvent(resource.itemIndex));
        });

        bool showMerge = resource.itemData.ItemType == ItemType.Weapon && WeaponLevelHelper.CanMerge(resource.colorDependencyNumber);
        mergeButton.gameObject.SetActive(showMerge);
        if (showMerge)
        {
            mergeButton.onClick.AddListener(() =>
            {
                GameEventBus.Publish(new InventoryItemMergeClickedEvent(resource.itemIndex));
            });
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
        Cleanup();
    }

    public void Cleanup()
    {
        sellButton.onClick.RemoveAllListeners();
        mergeButton.onClick.RemoveAllListeners();
    }

    private List<PropEntry> ToPropEntries(Dictionary<PropType, float> props)
    {
        List<PropEntry> entries = new();
        if (props == null)
        {
            return entries;
        }

        foreach (var kv in props)
        {
            entries.Add(new PropEntry(kv.Key, kv.Value));
        }

        return entries;
    }
}

public readonly struct InventoryItemOperateResource
{
    public readonly int itemIndex;
    public readonly ItemDataSO itemData;
    public readonly int colorDependencyNumber;
    public readonly int sellPrice;
    public readonly Dictionary<PropType, float> propDictionary;

    public InventoryItemOperateResource(
        int itemIndex,
        ItemDataSO itemData,
        int colorDependencyNumber,
        int sellPrice,
        Dictionary<PropType, float> propDictionary)
    {
        this.itemIndex = itemIndex;
        this.itemData = itemData;
        this.colorDependencyNumber = colorDependencyNumber;
        this.sellPrice = sellPrice;
        this.propDictionary = propDictionary;
    }
}
