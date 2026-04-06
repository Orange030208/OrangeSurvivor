using UnityEngine;
using UniversalUI.Core.Runtime;

namespace UniversalUI.Instances
{
    public class WaveTransitionUIPage : UIPageBase
    {
        [SerializeField] private UpgradeContainer[] upgradeContainers;
        [SerializeField] private Transform upgradeContainersParent;

        [Header("宝箱")]
        [SerializeField] private ChestAccessoryContainer chestAccessoryContainer;
        [SerializeField] private Transform chestContainerParent;

        protected override void OnPageOpened(UIPageOpenContext context)
        {
            GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
            GameEventBus.Subscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
            GameEventBus.Subscribe<WaveTransitionPhaseChanged>(OnWaveTransitionPhaseChanged);

            SetChestSelectionVisible(false);
            SetUpgradeSelectionVisible(false);
            GameEventBus.Publish<WaveTransitionSnapshot>();
        }

        protected override void OnPageClosed()
        {
            GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
            GameEventBus.Unsubscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
            GameEventBus.Unsubscribe<WaveTransitionPhaseChanged>(OnWaveTransitionPhaseChanged);

            chestAccessoryContainer.CleanUp();
            foreach (var container in upgradeContainers)
            {
                container.Cleanup();
            }
        }

        private void OnWaveTransitionPhaseChanged(WaveTransitionPhaseChanged eventData)
        {
            switch (eventData.newPhase)
            {
                case TransitionPhase.ChestSelection:
                    SetChestSelectionVisible(true);
                    SetUpgradeSelectionVisible(false);
                    break;
                case TransitionPhase.UpgradeSelection:
                    SetChestSelectionVisible(false);
                    SetUpgradeSelectionVisible(true);
                    break;
                default:
                    SetChestSelectionVisible(false);
                    SetUpgradeSelectionVisible(false);
                    break;
            }
        }

        private void ShowSelectAccessory(AccessorySelectionStartedEvent eventData)
        {
            if (!chestContainerParent.gameObject.activeSelf)
            {
                return;
            }

            chestAccessoryContainer.Configure(eventData.accessoryData);
        }

        private void OnUpgradeOptionsChanged(UpgradeOptionsChangedEvent eventData)
        {
            if (!upgradeContainersParent.gameObject.activeSelf || eventData.Props == null)
            {
                return;
            }

            int count = Mathf.Min(upgradeContainers.Length, eventData.Props.Length);
            for (int i = 0; i < count; i++)
            {
                UpgradeProp prop = eventData.Props[i];
                upgradeContainers[i].Configure(ResourcesManager.GetPropIcon(prop.propType), prop.propType.GetChineseName(), $"+{prop.value}%", prop.upgradeBonusCallback);
            }
        }

        private void SetChestSelectionVisible(bool visible)
        {
            chestContainerParent.gameObject.SetActive(visible);
            chestAccessoryContainer.gameObject.SetActive(visible);
            if (!visible)
            {
                chestAccessoryContainer.CleanUp();
            }
        }

        private void SetUpgradeSelectionVisible(bool visible)
        {
            upgradeContainersParent.gameObject.SetActive(visible);
            if (!visible)
            {
                foreach (var container in upgradeContainers)
                {
                    container.Cleanup();
                }
            }
        }
    }
}
