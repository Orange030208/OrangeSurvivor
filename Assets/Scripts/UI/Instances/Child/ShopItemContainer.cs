using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image[] colorDependencyImages;
    [SerializeField] private Image outline;
    [SerializeField] private Button itemButton;

    [Header("Prop管理")][SerializeField] private Transform propContainersParent;

    public event Action OnItemClicked;

    private void Awake()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        OnItemClicked?.Invoke();
    }

    public void Configure(ItemDataSO itemData, int level = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemDataSO is null in ShopItemContainer.Configure");
            return;
        }

        Color color;
        Dictionary<PropType, float> props;

        if (itemData is AccessoryDataSO accessoryData)
        {
            color = ColorHelper.GetColorByRarity(accessoryData.Rarity);
            props = accessoryData.GetProps();
        }
        else if (itemData is WeaponDataSO weaponData)
        {
            color = ColorHelper.GetColorByLevel(level);
            props = weaponData.GetPropsByLevel(level);
        }
        else
        {
            Debug.LogWarning($"Unsupported ItemDataSO type: {itemData.GetType().Name}");
            return;
        }

        ConfigureCommon(itemData, color, props);
    }

    private void ConfigureCommon(ItemDataSO itemData, Color color, Dictionary<PropType, float> props)
    {
        iconImage.sprite = itemData.ItemIcon;
        nameText.text = itemData.ItemName;
        priceText.text = itemData.ItemPrice.ToString();

        nameText.color = color;
        outline.color = color;

        foreach (var image in colorDependencyImages)
        {
            image.color = color;
        }

        PropContainerManager.GeneratePropContainers(props, propContainersParent);
    }
}
