using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class WaveTransitionUIPage : PageBase
{
    [Header("升级卡片")]
    [SerializeField] private WaveTransitionUpgradeCardGroup upgradeCardGroup;

    [Header("宝箱")]
    [SerializeField] private WaveTransitionChestPanel chestPanel;

    private CancellationTokenSource lifetimeCancellation;

    protected override void Awake()
    {
        base.Awake();
        ResolveViewParts();
        ValidateConfiguration();
    }

    protected override UniTask OnOpeningAsync(OpenContext context, CancellationToken cancellationToken)
    {
        ResetLifetimeCancellation();
        GameEventBus.Subscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Subscribe<UpgradeCardsRefreshOutRequestedEvent>(OnUpgradeCardsRefreshOutRequested);
        GameEventBus.Subscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Subscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        chestPanel.Hide();
        SetUpgradeSelectionVisible(false);
        GameEventBus.Publish(new RequestWaveTransitionStateSnapshotEvent());
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        CancelLifetimeOperations();
        GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Unsubscribe<UpgradeCardsRefreshOutRequestedEvent>(OnUpgradeCardsRefreshOutRequested);
        GameEventBus.Unsubscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Unsubscribe<WaveTransitionPhaseChangedEvent>(OnWaveTransitionPhaseChanged);

        chestPanel.Clear();
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
        chestPanel.Show(eventData.accessoryData);
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

            CancellationToken cancellationToken = ResolveLifetimeCancellationToken();
            await upgradeCardGroup.PlayRefreshOutAsync(cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            // 即使页面关闭导致动画取消，也要通知 Manager 清理 pending，避免刷新链路卡住。
            GameEventBus.Publish<UpgradeCardsRefreshOutCompletedEvent>();
        }
    }

    private CancellationToken ResolveLifetimeCancellationToken()
    {
        if (lifetimeCancellation == null)
        {
            ResetLifetimeCancellation();
        }

        return lifetimeCancellation.Token;
    }

    private void ResetLifetimeCancellation()
    {
        CancelLifetimeOperations();
        lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
    }

    private void CancelLifetimeOperations()
    {
        if (lifetimeCancellation == null)
        {
            return;
        }

        lifetimeCancellation.Cancel();
        lifetimeCancellation.Dispose();
        lifetimeCancellation = null;
    }

    private void SetChestSelectionVisible(bool visible)
    {
        chestPanel.SetVisible(visible);
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

    private void ResolveViewParts()
    {
        if (upgradeCardGroup == null)
        {
            upgradeCardGroup = GetComponentInChildren<WaveTransitionUpgradeCardGroup>(true);
        }

        if (chestPanel == null)
        {
            chestPanel = GetComponentInChildren<WaveTransitionChestPanel>(true);
        }
    }

    private void ValidateConfiguration()
    {
        if (upgradeCardGroup == null)
        {
            throw new MissingReferenceException($"{nameof(WaveTransitionUIPage)} '{name}' is missing upgrade card group.");
        }

        if (chestPanel == null)
        {
            throw new MissingReferenceException($"{nameof(WaveTransitionUIPage)} '{name}' is missing chest panel.");
        }
    }
}
