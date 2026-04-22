using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeContainer : UIContainerBase<InfoAddIndex<PropEntry>,Describer>
{
    public override void Configure(InfoAddIndex<PropEntry> resource)
    {
        iconImage.sprite = ResourcesManager.GetPropIcon(resource.info.propType);
        nameText.text = resource.info.GetDisplayName();
        //TODO:后续修改显示
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
