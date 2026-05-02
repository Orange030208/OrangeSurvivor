using System;
using AXR.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessoryOperateContainer : UIContainerBase<AccessoryDataSO, ExtraInfoDescriber>
{
    [SerializeField] private UIClickTarget takeButton;
    [SerializeField] private UIClickTarget recycleButton;
    [SerializeField] private TextMeshProUGUI recycleText;

    private AccessoryDataSO currentAccessory;

    public override void Configure(AccessoryDataSO resource)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource), $"{nameof(AccessoryOperateContainer)} '{name}' received a null accessory resource.");
        }

        nameText.text = resource.ItemName;
        iconImage.sprite = resource.ItemIcon;
        recycleText.text = resource.RecyclePrice.ToString();
        RenderItemQuality(resource, resource.Rarity);
        bottom.Display(resource);

        takeButton.OnClicked -= OnTakeButtonClicked;
        recycleButton.OnClicked -= OnRecycleButtonClicked;

        currentAccessory = resource;

        takeButton.OnClicked += OnTakeButtonClicked;
        recycleButton.OnClicked += OnRecycleButtonClicked;
    }

    private void OperateAccessory(AccessoryDataSO accessoryData, bool selected)
    {
        GameEventBus.Publish(new AccessoryOperateEvent(accessoryData, selected));
    }

    private void OnTakeButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        OperateAccessory(currentAccessory, true);
    }

    private void OnRecycleButtonClicked()
    {
        AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
        OperateAccessory(currentAccessory, false);
    }

    public override void Dispose()
    {
        base.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        takeButton.OnClicked -= OnTakeButtonClicked;
        recycleButton.OnClicked -= OnRecycleButtonClicked;
        currentAccessory = null;
    }
}
