using UnityEngine;
using UnityEngine.UI;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WeaponSelectionUIPage : UIPageBase
    {
        [SerializeField]private Button startButton;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            startButton.onClick.AddListener(GameManager.Instance.StartGame);
        }

        protected override void OnPageClosed()
        {
            startButton.onClick.RemoveListener(GameManager.Instance.StartGame);
        }
    }
}