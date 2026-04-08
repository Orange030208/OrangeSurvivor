using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemOperateContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [Header("根据稀有度或者等级改变颜色的组件")]
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button mergeButton;

    [Header("Prop管理")] [SerializeField] private Transform propContainersParent;

    public void Configure(
        ItemDataSO itemData,
        int colorDependencyNumber,
        Dictionary<PropType, float> propDictionary,
        Action onSell,
        Action onMerge,
        Action onClose)
    {
        iconImage.sprite = itemData.ItemIcon;

        if (itemData.ItemType == ItemType.Weapon)
        {
            nameText.text = ItemDisplayHelper.GetWeaponDisplayName(itemData.ItemName, colorDependencyNumber);
        }
        else
        {
            nameText.text = itemData.ItemName;
        }

        priceText.text = itemData.ItemPrice.ToString();

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

        if (propContainersParent != null && propDictionary != null)
        {
            PropContainerManager.GeneratePropContainers(propDictionary, propContainersParent);
        }

        sellButton.onClick.RemoveAllListeners();
        mergeButton.onClick.RemoveAllListeners();

        sellButton.onClick.AddListener(() => onSell?.Invoke());

        bool showMerge = itemData.ItemType == ItemType.Weapon;
        mergeButton.gameObject.SetActive(showMerge);
        if (showMerge)
        {
            mergeButton.onClick.AddListener(() => onMerge?.Invoke());
        }
    }

    public void Cleanup()
    {
        sellButton.onClick.RemoveAllListeners();
        mergeButton.onClick.RemoveAllListeners();
    }
}
