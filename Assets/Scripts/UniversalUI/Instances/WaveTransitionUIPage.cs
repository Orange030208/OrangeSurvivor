using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WaveTransitionUIPage : UIPageBase
    {
        [SerializeField] private Button[] upgradeButtons;
        [SerializeField] private TextMeshProUGUI[] upgradeTexts;
        
        private int _btnCount = 3;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            PropEnum[] props = FetchUpgradeProps();
            for (int i = 0; i < _btnCount; i++)
            {
                upgradeTexts[i].text = props[i].FormatPropName();
            }
        }
        
        //ui来的时候主动拉一次数据，防止逻辑先于UI
        private PropEnum[] FetchUpgradeProps()
        {
            return WaveTransitionManager.Instance.PropEnums;
        }
    }
}
