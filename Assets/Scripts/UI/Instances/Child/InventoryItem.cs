using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IDisposable
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private UIClickTarget button;

    private int itemIndex = -1;
    private ItemDataSO currentItemData;
    private int currentColorDependencyNumber;
    private IDescribable describable;

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

        button.OnClicked -= OnItemClicked;
        button.OnClicked += OnItemClicked;
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
