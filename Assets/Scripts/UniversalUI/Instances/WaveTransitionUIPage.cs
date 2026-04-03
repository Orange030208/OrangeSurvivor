using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WaveTransitionUIPage : UIPageBase
    {
        [SerializeField] private UpgradeContainer[] upGradeContainers;
        
        protected override void OnPageOpened(UIPageOpenContext context)
        {
            UpgradeProp[] props = FetchUpgradeProps();
            for (int i = 0; i < 3; i++)
            {
                upGradeContainers[i].Configure(null, props[i].propType.FormatPropName(), $"+{props[i].value}%",props[i].upgradeBonusCallback);
            }

            WaveTransitionManager.Instance.OnUpdatePropsChanged += OnUpdatePropsChanged;
        }

        private UpgradeProp[] FetchUpgradeProps()
        {
            return WaveTransitionManager.Instance.UpgradeProps;
        }

        private void OnUpdatePropsChanged(UpgradeProp[] props)
        {
            for (int i = 0; i < 3; i++)
            {
                upGradeContainers[i].Configure(null, props[i].propType.FormatPropName(), $"+{props[i].value}%",props[i].upgradeBonusCallback);
            }
        }

        protected override void OnPageClosed()
        {
            WaveTransitionManager.Instance.OnUpdatePropsChanged -= OnUpdatePropsChanged;
        }
    }
}

[Serializable]
public class UpgradeContainer
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI upgradeNameText;
    [SerializeField] private TextMeshProUGUI upgradeValueText;

    [field:SerializeField]public Button Button {private set; get; }

    public void Configure(Sprite icon, string upgradeName, string upgradeValue,Action buttonAction)
    {
        image.sprite = icon;
        upgradeNameText.text = upgradeName;
        upgradeValueText.text = upgradeValue;
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => buttonAction?.Invoke());
    }
}
