using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 即时奖励选择管理器：负责宝箱饰品与升级卡片奖励的展示、应用和完成通知。
/// </summary>
public class RewardSelectionManager : MonoBehaviour
{
    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private Player player;
    [SerializeField] private CurrencyWallet currencyWallet;
    [SerializeField] private UpgradeCardPoolSO upgradeCardPool;

    private readonly UpgradeRunState upgradeRunState = new();
    private readonly UpgradeCardRollService upgradeCardRollService = new();
    private readonly UpgradeCardApplyService upgradeCardApplyService = new();
    private UpgradeCardSO[] upgradeCardOptions = Array.Empty<UpgradeCardSO>();
    private UpgradeCardOptionSnapshot[] upgradeOptionSnapshots = Array.Empty<UpgradeCardOptionSnapshot>();
    private AccessoryDataSO currentAccessoryData;
    private CancellationTokenSource refreshUpgradeCardsCancellation;
    private int currentWaveNumber = 1;
    private PlayerLevel playerLevel;
    private RewardSelectionReason currentReason = RewardSelectionReason.None;
    private RewardSelectionPhase currentPhase = RewardSelectionPhase.None;

    private RewardSelectionPhase CurrentPhase
    {
        get => currentPhase;
        set
        {
            if (currentPhase == value)
            {
                return;
            }

            RewardSelectionPhase oldPhase = currentPhase;
            currentPhase = value;
            GameEventBus.Publish(new RewardSelectionPhaseChangedEvent(oldPhase, currentPhase));
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestRewardSelectionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Subscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);

        TryBindPlayerReferences();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestRewardSelectionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Unsubscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);

        CancelRefreshUpgradeCards();
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.RewardSelection)
        {
            StartTransitionFlow();
            return;
        }

        if (eventData.OldState == GameState.RewardSelection)
        {
            currentAccessoryData = null;
            currentReason = RewardSelectionReason.None;
            CurrentPhase = RewardSelectionPhase.None;
            CancelRefreshUpgradeCards();
        }
    }

    private void OnWaveStarted(WaveStartedEvent eventData)
    {
        currentWaveNumber = Mathf.Max(1, eventData.CurrentWave);
    }

    private void OnWaveRuntimeChanged(WaveRuntimeChangedEvent eventData)
    {
        if (eventData.CurrentWave > 0)
        {
            currentWaveNumber = eventData.CurrentWave;
        }
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
        accessoryManager = player.GetComponent<AccessoryManager>();
        playerLevel = player.GetComponent<PlayerLevel>();
        currencyWallet = player.GetComponent<CurrencyWallet>();
    }

    private void StartTransitionFlow()
    {
        currentAccessoryData = null;
        CancelRefreshUpgradeCards();
        upgradeCardOptions = Array.Empty<UpgradeCardSO>();
        upgradeOptionSnapshots = Array.Empty<UpgradeCardOptionSnapshot>();
        CurrentPhase = RewardSelectionPhase.None;
        TryBindPlayerReferences();
        currentReason = ResolveCurrentReason();
        TryEnterNextPhase();
    }

    private void TryEnterNextPhase()
    {
        switch (currentReason)
        {
            case RewardSelectionReason.Chest:
                EnterChestSelection();
                break;
            case RewardSelectionReason.Upgrade:
                EnterUpgradeSelection();
                break;
            default:
                CompleteRewardSelection();
                break;
        }
    }

    private void EnterChestSelection()
    {
        CurrentPhase = RewardSelectionPhase.ChestSelection;
        currentAccessoryData = ResourcesManager.GetRandomAccessory();
        GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
    }

    private void OnAccessoryOperated(AccessoryOperateEvent eventData)
    {
        if (CurrentPhase != RewardSelectionPhase.ChestSelection)
        {
            return;
        }

        if (currentAccessoryData == null || eventData.accessoryData != currentAccessoryData)
        {
            return;
        }

        if (eventData.selected)
        {
            accessoryManager?.EquipAccessory(eventData.accessoryData);
            print($"选择了{eventData.accessoryData.ItemName}");
        }
        else
        {
            currencyWallet?.ChangeAmount(eventData.accessoryData.RecyclePrice);
            print($"回收了{eventData.accessoryData.ItemName},回收价格:{eventData.accessoryData.RecyclePrice}");
        }

        currentAccessoryData = null;
        CompleteRewardSelection();
    }

    private void EnterUpgradeSelection()
    {
        currentAccessoryData = null;

        if (playerLevel == null || playerLevel.UnspentUpgradePoints <= 0)
        {
            CompleteRewardSelection();
            return;
        }

        CurrentPhase = RewardSelectionPhase.UpgradeSelection;
        ConfigureUpgradeCards();
    }

    [NaughtyAttributes.Button("刷新升级卡片")]
    private void RefreshUpgradeCards()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[RewardSelectionManager] Refresh upgrade cards can only run in Play Mode.", this);
            return;
        }

        if (CurrentPhase != RewardSelectionPhase.UpgradeSelection)
        {
            Debug.LogWarning("[RewardSelectionManager] Upgrade cards can only be refreshed during UpgradeSelection phase.", this);
            return;
        }

        if (refreshUpgradeCardsCancellation != null)
        {
            return;
        }

        RefreshUpgradeCardsWithMotionAsync().Forget();
    }

    private async UniTaskVoid RefreshUpgradeCardsWithMotionAsync()
    {
        refreshUpgradeCardsCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        try
        {
            RewardSelectionUpgradeCardGroup cardGroup = FindFirstObjectByType<RewardSelectionUpgradeCardGroup>();
            if (cardGroup != null)
            {
                await cardGroup.PlayRefreshOutAsync(refreshUpgradeCardsCancellation.Token);
            }

            ConfigureUpgradeCards();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CancelRefreshUpgradeCards();
        }
    }

    private void ConfigureUpgradeCards()
    {
        if (CurrentPhase != RewardSelectionPhase.UpgradeSelection)
        {
            return;
        }

        UpgradeCardPoolSO pool = ResolveUpgradeCardPool();
        UpgradeCardOfferContext offerContext = new(
            upgradeRunState,
            currentWaveNumber,
            player != null ? player.GetComponent<WeaponsHolder>() : null);
        var rolledOptions = upgradeCardRollService.RollOptions(pool, offerContext);
        if (rolledOptions.Count == 0)
        {
            Debug.LogWarning("[RewardSelectionManager] No upgrade cards could be rolled. Completing reward selection.");
            CompleteRewardSelection();
            return;
        }

        upgradeCardOptions = rolledOptions.ToArray();
        upgradeOptionSnapshots = new UpgradeCardOptionSnapshot[upgradeCardOptions.Length];
        for (int i = 0; i < upgradeCardOptions.Length; i++)
        {
            upgradeOptionSnapshots[i] = upgradeCardOptions[i].ToSnapshot(upgradeRunState);
        }

        GameEventBus.Publish(new UpgradeOptionsChangedEvent(upgradeOptionSnapshots));
    }

    private UpgradeCardPoolSO ResolveUpgradeCardPool()
    {
        if (upgradeCardPool == null)
        {
            upgradeCardPool = ResourcesManager.GetUpgradeCardPool();
        }

        return upgradeCardPool;
    }

    private void CompleteRewardSelection()
    {
        currentReason = RewardSelectionReason.None;
        CurrentPhase = RewardSelectionPhase.None;
        GameEventBus.Publish<RewardSelectionCompletedEvent>();
    }

    private void ContinueOrCompleteUpgradeSelection()
    {
        if (CurrentPhase != RewardSelectionPhase.UpgradeSelection)
        {
            return;
        }

        int remainingUpgradePoints = playerLevel.ConsumeUpgradePoint();
        if (remainingUpgradePoints > 0)
        {
            ConfigureUpgradeCards();
            return;
        }

        CompleteRewardSelection();
    }

    private void OnUpgradeContainerClicked(UpgradeContainerClickedEvent eventData)
    {
        if (CurrentPhase != RewardSelectionPhase.UpgradeSelection)
        {
            return;
        }

        if (eventData.ContainerIndex < 0 || eventData.ContainerIndex >= upgradeCardOptions.Length)
        {
            Debug.LogWarning($"[RewardSelectionManager] Invalid upgrade card index {eventData.ContainerIndex}.");
            return;
        }

        UpgradeCardSO selectedCard = upgradeCardOptions[eventData.ContainerIndex];
        if (!upgradeCardApplyService.Apply(selectedCard, player))
        {
            Debug.LogWarning($"[RewardSelectionManager] Failed to apply upgrade card {selectedCard?.name}.");
            return;
        }

        upgradeRunState.RecordPick(selectedCard);
        ContinueOrCompleteUpgradeSelection();
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new RewardSelectionPhaseChangedEvent(RewardSelectionPhase.None, CurrentPhase));

        switch (CurrentPhase)
        {
            case RewardSelectionPhase.ChestSelection:
                if (currentAccessoryData != null)
                {
                    GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
                }

                break;
            case RewardSelectionPhase.UpgradeSelection:
                GameEventBus.Publish(new UpgradeOptionsChangedEvent(upgradeOptionSnapshots));
                break;
        }
    }

    private void TryBindPlayerReferences()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (player == null)
        {
            accessoryManager = null;
            playerLevel = null;
            currencyWallet = null;
            return;
        }

        if (accessoryManager == null)
        {
            accessoryManager = player.GetComponent<AccessoryManager>();
        }

        if (playerLevel == null)
        {
            playerLevel = player.GetComponent<PlayerLevel>();
        }

        if (currencyWallet == null)
        {
            currencyWallet = player.GetComponent<CurrencyWallet>();
        }

    }

    private void CancelRefreshUpgradeCards()
    {
        if (refreshUpgradeCardsCancellation == null)
        {
            return;
        }

        refreshUpgradeCardsCancellation.Cancel();
        refreshUpgradeCardsCancellation.Dispose();
        refreshUpgradeCardsCancellation = null;
    }

    private static RewardSelectionReason ResolveCurrentReason()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null
            ? gameManager.CurrentRewardSelectionReason
            : RewardSelectionReason.None;
    }
}
