using UnityEngine;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public sealed class MenuUIPage : UIPageBase
    {
        [SerializeField]private Button startButton;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            startButton.onClick.AddListener(GameManager.Instance.WeaponSelection);
        }

        protected override void OnPageClosed()
        {
            startButton.onClick.RemoveListener(GameManager.Instance.WeaponSelection);
        }
    }
}
