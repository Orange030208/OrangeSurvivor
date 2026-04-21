using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemContainer : MonoBehaviour, IDisposable, IDisplayDocumentSource
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private UIClickTarget button;
    [SerializeField] private TooltipHoverTarget tooltipHoverTarget;

    private int itemIndex = -1;
    private ItemDataSO currentItemData;
    private int currentColorDependencyNumber;

    public void Configure(ItemDataSO itemData, int colorDependencyNumber, int itemIndex)
    {
        this.itemIndex = itemIndex;
        currentItemData = itemData;
        currentColorDependencyNumber = colorDependencyNumber;

        iconImage.sprite = itemData.ItemIcon;
        foreach (Graphic g in colorDependencyGraphics)
        {
            switch (itemData.ItemType)
            {
                case ItemType.Accessory:
                    g.color = ColorHelper.GetColorByRarity(colorDependencyNumber);
                    break;
                case ItemType.Weapon:
                    g.color = ColorHelper.GetColorByLevel(colorDependencyNumber);
                    break;
                default:
                    Debug.LogWarning($"需要配置{itemData.ItemType}的颜色");
                    break;
            }
        }

        tooltipHoverTarget?.SetTooltipDataSource(this);

        button.OnClicked -= OnItemClicked;
        button.OnClicked += OnItemClicked;
    }

    public DisplayDocument BuildDisplayDocument()
    {
        return TooltipDisplayDocumentBuilder.CreateFromItem(currentItemData, currentColorDependencyNumber);
    }

    private void OnItemClicked()
    {
        if (itemIndex < 0)
        {
            return;
        }

        GameEventBus.Publish(new InventoryItemClickedEvent(itemIndex));
    }

    public void Dispose()
    {
        button.OnClicked -= OnItemClicked;
        currentItemData = null;
        currentColorDependencyNumber = 0;
        itemIndex = -1;
    }
}
