using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WaveTransitionUIPage : UIPageBase
    {
        [SerializeField] private UpgradeContainer[] upgradeContainers;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
            GameEventBus.Publish<RequestUpgradeOptionsSnapshotEvent>();
        }

        protected override void OnPageClosed()
        {
            GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        }

        private void OnUpgradeOptionsChanged(UpgradeOptionsChangedEvent e)
        {
            if (e.Props == null) return;

            int count = Mathf.Min(upgradeContainers.Length, e.Props.Length);
            for (int i = 0; i < count; i++)
            {
                UpgradeProp prop = e.Props[i];
                upgradeContainers[i].Configure(null, prop.propType.FormatPropName(), $"+{prop.value}%", prop.upgradeBonusCallback);
            }
        }
    }
}

[Serializable]
public class UpgradeContainer
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeValueText;

    [field: SerializeField] public Button Button { private set; get; }

    public void Configure(Sprite icon, string upgradeName, string upgradeValue, Action buttonAction)
    {
        image.sprite = icon;
        upgradeNameText.text = upgradeName;
        upgradeValueText.text = upgradeValue;
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => buttonAction?.Invoke());
    }
}
