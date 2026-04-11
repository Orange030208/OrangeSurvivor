using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessoryOperateContainer : UIContainerBase<AccessoryDataSO, CharacterExtraInfoDisplayer>
{
    [SerializeField] private Button takeButton;
    [SerializeField] private Button recycleButton;
    [SerializeField] private TextMeshProUGUI recycleText;
    [SerializeField] private Image outline;

    public override void Configure(AccessoryDataSO resource)
    {
        if (resource == null)
        {
            return;
        }

        nameText.text = resource.ItemName;
        recycleText.text = resource.RecyclePrice.ToString();
        RenderColor(resource, resource.Rarity);
        bottom.DisplayDescriptions(resource.GetAutoDescriptions());

        takeButton.onClick.RemoveAllListeners();
        recycleButton.onClick.RemoveAllListeners();

        takeButton.onClick.AddListener(() => OperateAccessory(resource, true));
        recycleButton.onClick.AddListener(() => OperateAccessory(resource, false));
    }

    private void OperateAccessory(AccessoryDataSO accessoryData, bool selected)
    {
        GameEventBus.Publish(new AccessoryOperateEvent(accessoryData, selected));
    }

    public override void Dispose()
    {
        base.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        takeButton.onClick.RemoveAllListeners();
        recycleButton.onClick.RemoveAllListeners();
    }
}
