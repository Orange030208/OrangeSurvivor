using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestAccessoryContainer : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI accessoryNameText;
    [SerializeField] private Button takeButton;
    [SerializeField] private Button recycleButton;
    [SerializeField] private Image[] rarityDependencyImages;
    [SerializeField] private Image outline;

    [Header("Prop管理")] [SerializeField] private Transform propContainersParent;

    public void Configure(AccessoryDataSO accessoryData)
    {
        iconImage.sprite = accessoryData.Icon;
        accessoryNameText.text = accessoryData.DisplayName;

        Color color = ColorHelper.GetColorByRarity(accessoryData.Rarity);
        accessoryNameText.color = color;

        outline.color = color;

        foreach (var image in rarityDependencyImages)
        {
            image.color = color;
        }
        
        takeButton.onClick.RemoveAllListeners();
        recycleButton.onClick.RemoveAllListeners();

        takeButton.onClick.AddListener(()=>OperateAccessory(accessoryData, true));
        recycleButton.onClick.AddListener(()=>OperateAccessory(accessoryData, false));

        ConfigurePropContainer(accessoryData.GetPropertyModifiers());
    }

    private void OperateAccessory(AccessoryDataSO accessoryData,bool selected)
    {
        GameEventBus.Publish(new AccessoryOperateEvent(accessoryData,selected));
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