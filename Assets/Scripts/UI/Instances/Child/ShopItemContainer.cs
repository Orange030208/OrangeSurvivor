using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [Header("根据稀有度或者等级改变颜色的组件")]
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private Image outline;
    [SerializeField] private Button itemButton;

    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockSprite, unlockSprite;

    [Header("Prop管理")][SerializeField] private Transform propContainersParent;

    public event Action OnItemClicked;
    public event Action OnLockClicked;

    private void OnEnable()
    {
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnButtonLockClicked);
        }
    }

    private void OnDisable()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(OnButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.RemoveListener(OnButtonLockClicked);
        }
    }

    private void OnButtonLockClicked()
    {
        OnLockClicked?.Invoke();
    }

    private void OnButtonClicked()
    {
        OnItemClicked?.Invoke();
    }

    public void Configure(ItemDataSO itemData, bool isLocked, int level = 1)
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

        lockImage.sprite = isLocked ? lockSprite : unlockSprite;

        ConfigureCommon(itemData, color, props);
    }

    private void ConfigureCommon(ItemDataSO itemData, Color color, Dictionary<PropType, float> props)
    {
        iconImage.sprite = itemData.ItemIcon;
        nameText.text = itemData.ItemName;
        priceText.text = itemData.ItemPrice.ToString();

        foreach (var image in colorDependencyGraphics)
        {
            image.color = color;
        }

        PropContainerManager.GeneratePropContainers(props, propContainersParent);
    }

    public void CleanUp()
    {
        OnItemClicked = null;
        OnLockClicked = null;
    }
}
