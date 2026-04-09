using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeContainer : UIContainerBase<InfoAddIndex<PropEntry>,TextMeshProUGUI>
{
    public override void Configure(InfoAddIndex<PropEntry> resource)
    {
        iconImage.sprite = ResourcesManager.GetPropIcon(resource.info.propType);
        nameText.text = resource.info.propType.GetChineseName();
        bottom.text = resource.info.value.ToString();
        CleanClickEvent();
        OnClicked += _ =>
        {
            GameEventBus.Publish<UpgradeContainerClickedEvent>(new UpgradeContainerClickedEvent(resource.index));
        };
    }
}

public struct InfoAddIndex<T>
{
    public T info;
    public int index;

    public InfoAddIndex(T info, int index)
    {
        this.info = info;
        this.index = index;
    }
}