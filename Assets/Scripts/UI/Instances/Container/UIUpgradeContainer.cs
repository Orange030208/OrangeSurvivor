using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeContainer : UIContainerBase<InfoAddIndex<PropModifierData>,Describer>
{
    public override void Configure(InfoAddIndex<PropModifierData> resource)
    {
        iconImage.sprite = ResourcesManager.GetPropIcon(resource.info.propType);
        nameText.text = resource.info.GetDisplayName();
        DefaultDescribe describable = new DefaultDescribe
        {
            Description = resource.info.GetAutoDescription()
        };
        bottom.Display(describable);
        CleanClickEvent();
        OnClicked += _ =>
        {
            AudioSfxBridge.RequestPlay(AudioSfxKey.WoodenButtonClicked);
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
