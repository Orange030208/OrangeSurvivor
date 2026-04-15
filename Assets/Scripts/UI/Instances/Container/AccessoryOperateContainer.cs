using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessoryOperateContainer : UIContainerBase<AccessoryDataSO, DescriptionListDisplayer>
{
    [SerializeField] private UIClickTarget takeButton;
    [SerializeField] private UIClickTarget recycleButton;
    [SerializeField] private TextMeshProUGUI recycleText;
    [SerializeField] private Image outline;

    private AccessoryDataSO currentAccessory;

    public override void Configure(AccessoryDataSO resource)
    {
        if (resource == null)
        {
            return;
        }

        nameText.text = resource.ItemName;
        recycleText.text = resource.RecyclePrice.ToString();
        RenderColor(resource, resource.Rarity);
        bottom.DisplayDescriptions(resource.GetDescriptions());

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
        OperateAccessory(currentAccessory, true);
    }

    private void OnRecycleButtonClicked()
    {
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
