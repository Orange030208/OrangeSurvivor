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
