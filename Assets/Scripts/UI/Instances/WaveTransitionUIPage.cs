using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class WaveTransitionUIPage : PageBase
{
    [Header("升级卡片")]
    [SerializeField] private WaveTransitionUpgradeCardGroup upgradeCardGroup;

    [Header("宝箱")]
    [SerializeField] private AccessoryOperateContainer accessoryOperateContainer;
    [SerializeField] private Transform chestContainerParent;

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Subscribe<UpgradeCardsRefreshOutRequestedEvent>(OnUpgradeCardsRefreshOutRequested);
        GameEventBus.Subscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Subscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        SetChestSelectionVisible(false);
        SetUpgradeSelectionVisible(false);
        GameEventBus.Publish(new RequestWaveTransitionStateSnapshotEvent());
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Unsubscribe<UpgradeCardsRefreshOutRequestedEvent>(OnUpgradeCardsRefreshOutRequested);
        GameEventBus.Unsubscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Unsubscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        accessoryOperateContainer.CleanUp();
        upgradeCardGroup?.Clear();
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
        if (upgradeCardGroup == null)
        {
            Debug.LogError($"{nameof(WaveTransitionUIPage)} missing {nameof(upgradeCardGroup)}.", this);
            return;
        }

        upgradeCardGroup.Configure(eventData.Options);
    }

    private void OnUpgradeCardsRefreshOutRequested()
    {
        PlayUpgradeCardsRefreshOutAsync().Forget();
    }

    private async UniTaskVoid PlayUpgradeCardsRefreshOutAsync()
    {
        try
        {
            if (upgradeCardGroup == null)
            {
                Debug.LogError($"{nameof(WaveTransitionUIPage)} missing {nameof(upgradeCardGroup)}.", this);
                return;
            }

            await upgradeCardGroup.PlayRefreshOutAsync(this.GetCancellationTokenOnDestroy());
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            GameEventBus.Publish<UpgradeCardsRefreshOutCompletedEvent>();
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
        if (upgradeCardGroup == null)
        {
            Debug.LogError($"{nameof(WaveTransitionUIPage)} missing {nameof(upgradeCardGroup)}.", this);
            return;
        }

        upgradeCardGroup.SetVisible(visible);
    }

}
