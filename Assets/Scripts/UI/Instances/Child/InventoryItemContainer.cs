using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic[] colorDependencyGraphics;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="colorDependencyNumber">武器传等级，饰品传稀有度</param>
    public void Configure(ItemDataSO itemData, int colorDependencyNumber)
    {
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
    }
}