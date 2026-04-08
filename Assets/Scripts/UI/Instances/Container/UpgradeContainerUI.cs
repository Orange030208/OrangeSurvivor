using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeContainerUI : ContainerBaseUI<UpgradeContainerUI.UpgradeContainerUIConfigure>
{
    [SerializeField] private TextMeshProUGUI upgradeValueText;

    public override void Configure(UpgradeContainerUIConfigure resource)
    {
        iconImage.sprite = ResourcesManager.GetPropIcon(resource.propEntry.propType);
        nameText.text = resource.propEntry.propType.GetChineseName();
        upgradeValueText.text = resource.propEntry.value.ToString();
        CleanClickEvent();
        OnClicked += _ =>
        {
            GameEventBus.Publish<UpgradeContainerClickedEvent>(new UpgradeContainerClickedEvent(resource.index));
        };
    }
    
    public struct UpgradeContainerUIConfigure
    {
        public PropEntry propEntry;
        public int index;

        public UpgradeContainerUIConfigure(PropEntry propEntry, int index)
        {
            this.propEntry = propEntry;
            this.index = index;
        }
    }
}