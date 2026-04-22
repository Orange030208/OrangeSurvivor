using UnityEngine;

public class WaveTransitionUIPage : UIPageBase
{
    [SerializeField] private UIUpgradeContainer[] upgradeContainers;
    [SerializeField] private Transform upgradeContainersParent;

    [Header("宝箱")]
    [SerializeField] private AccessoryOperateContainer accessoryOperateContainer;
    [SerializeField] private Transform chestContainerParent;

    protected override void OnPageOpened(UIPageOpenContext context)
    {
        GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Subscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Subscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        SetChestSelectionVisible(false);
        SetUpgradeSelectionVisible(false);
        GameEventBus.Publish(new RequestWaveTransitionStateSnapshotEvent());
    }

    protected override void OnPageClosed()
    {
        GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Unsubscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Unsubscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        accessoryOperateContainer.CleanUp();
    }

    private void OnWaveTransitionPhaseChanged(WaveTransitionPhaseChangedEvent eventData)
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
        accessoryOperateContainer.Configure(eventData.accessoryData);
    }

    private void OnUpgradeOptionsChanged(UpgradeOptionsChangedEvent eventData)
    {
        for (int i = 0; i < eventData.PropEntries.Length; i++)
        {
            PropEntry prop = eventData.PropEntries[i];
            upgradeContainers[i].Configure(new InfoAddIndex<PropEntry>(prop, i));
        }
    }

    private void SetChestSelectionVisible(bool visible)
    {
        chestContainerParent.gameObject.SetActive(visible);
        accessoryOperateContainer.gameObject.SetActive(visible);
        if (!visible)
        {
            accessoryOperateContainer.CleanUp();
        }
    }

    private void SetUpgradeSelectionVisible(bool visible)
    {
        upgradeContainersParent.gameObject.SetActive(visible);
    }
}
