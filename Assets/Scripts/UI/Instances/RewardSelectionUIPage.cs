using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

public class RewardSelectionUIPage : PageBase
{
    [Header("升级卡片")]
    [SerializeField] private RewardSelectionUpgradeCardGroup upgradeCardGroup;

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
        GameEventBus.Subscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Subscribe<RewardSelectionPhaseChangedEvent>(OnRewardSelectionPhaseChanged);

        chestPanel.Hide();
        SetUpgradeSelectionVisible(false);
        GameEventBus.Publish(new RequestRewardSelectionStateSnapshotEvent());
        return UniTask.CompletedTask;
    }

    protected override void OnClosed(CloseReason reason)
    {
        CancelLifetimeOperations();
        GameEventBus.Unsubscribe<UpgradeOptionsChangedEvent>(OnUpgradeOptionsChanged);
        GameEventBus.Unsubscribe<AccessorySelectionStartedEvent>(ShowSelectAccessory);
        GameEventBus.Unsubscribe<RewardSelectionPhaseChangedEvent>(OnRewardSelectionPhaseChanged);

        chestPanel.Clear();
        upgradeCardGroup?.Clear();
    }

    private void OnRewardSelectionPhaseChanged(RewardSelectionPhaseChangedEvent eventData)
    {
        switch (eventData.newPhase)
        {
            case RewardSelectionPhase.ChestSelection:
                SetChestSelectionVisible(true);
                SetUpgradeSelectionVisible(false);
                break;
            case RewardSelectionPhase.UpgradeSelection:
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
            Debug.LogError($"{nameof(RewardSelectionUIPage)} missing {nameof(upgradeCardGroup)}.", this);
            return;
        }

        upgradeCardGroup.Configure(eventData.Options);
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
            Debug.LogError($"{nameof(RewardSelectionUIPage)} missing {nameof(upgradeCardGroup)}.", this);
            return;
        }

        upgradeCardGroup.SetVisible(visible);
    }

    private void ResolveViewParts()
    {
        if (upgradeCardGroup == null)
        {
            upgradeCardGroup = GetComponentInChildren<RewardSelectionUpgradeCardGroup>(true);
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
            throw new MissingReferenceException($"{nameof(RewardSelectionUIPage)} '{name}' is missing upgrade card group.");
        }

        if (chestPanel == null)
        {
            throw new MissingReferenceException($"{nameof(RewardSelectionUIPage)} '{name}' is missing chest panel.");
        }
    }
}
