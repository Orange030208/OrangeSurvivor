using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessoryOperateContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI accessoryNameText;
    [SerializeField] private Button takeButton;
    [SerializeField] private Button recycleButton;
    [SerializeField] private TextMeshProUGUI recyclePriceText;
    [Header("根据稀有度改变颜色的组件")]
    [SerializeField] private Graphic[] colorDependencyGraphics;
    [SerializeField] private Image outline;

    [Header("Prop管理")] [SerializeField] private Transform propContainersParent;

    public void Configure(AccessoryDataSO accessoryData)
    {
        iconImage.sprite = accessoryData.ItemIcon;
        accessoryNameText.text = accessoryData.ItemName;
        recyclePriceText.text = accessoryData.RecyclePrice.ToString();

        Color color = ColorHelper.GetColorByRarity(accessoryData.Rarity);

        foreach (var image in colorDependencyGraphics)
        {
            image.color = color;
        }

        takeButton.onClick.RemoveAllListeners();
        recycleButton.onClick.RemoveAllListeners();

        takeButton.onClick.AddListener(() => OperateAccessory(accessoryData, true));
        recycleButton.onClick.AddListener(() => OperateAccessory(accessoryData, false));

        ConfigurePropContainer(accessoryData.GetProps());
    }

    private void OperateAccessory(AccessoryDataSO accessoryData, bool selected)
    {
        GameEventBus.Publish(new AccessoryOperateEvent(accessoryData, selected));
    }

    private void ConfigurePropContainer(Dictionary<PropType, float> calculatedProps)
    {
        PropContainerManager.GeneratePropContainers(calculatedProps, propContainersParent);
    }

    public void CleanUp()
    {
        takeButton.onClick.RemoveAllListeners();
        recycleButton.onClick.RemoveAllListeners();
    }
}
