using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemContainer : MonoBehaviour, IDisposable
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private Button button;

    private int itemIndex = -1;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="colorDependencyNumber">武器传等级，饰品传稀有度</param>
    /// <param name="itemIndex">背包UI中的下标</param>
    public void Configure(ItemDataSO itemData, int colorDependencyNumber, int itemIndex)
    {
        this.itemIndex = itemIndex;

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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnItemClicked);
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
        button.onClick.RemoveAllListeners();
        itemIndex = -1;
    }
}
