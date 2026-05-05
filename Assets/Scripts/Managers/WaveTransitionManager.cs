using System;
using System.Collections;
using UnityEngine;

public enum TransitionPhase
{
    None,
    ChestSelection,
    UpgradeSelection
}

/// <summary>
/// 波次过渡管理器，负责在波次之间提供玩家属性升级选项。
/// </summary>
public class WaveTransitionManager : MonoBehaviour
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
    private Coroutine refreshUpgradeCardsRoutine;
    private int latestCompletedWave = 1;
    private PlayerLevel playerLevel;
    private TransitionPhase currentPhase = TransitionPhase.None;

    private TransitionPhase CurrentPhase
    {
        get => currentPhase;
        set
        {
            if (currentPhase == value)
            {
                return;
            }

            TransitionPhase oldPhase = currentPhase;
            currentPhase = value;
            GameEventBus.Publish(new WaveTransitionPhaseChangedEvent(oldPhase, currentPhase));
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Subscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Subscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);

        TryBindPlayerReferences();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWaveTransitionStateSnapshotEvent>(PublishSnapshot);
        GameEventBus.Unsubscribe<AccessoryOperateEvent>(OnAccessoryOperated);
        GameEventBus.Unsubscribe<UpgradeContainerClickedEvent>(OnUpgradeContainerClicked);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);

        if (refreshUpgradeCardsRoutine != null)
        {
            StopCoroutine(refreshUpgradeCardsRoutine);
            refreshUpgradeCardsRoutine = null;
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.WaveTransition)
        {
            StartTransitionFlow();
            return;
        }

        if (eventData.OldState == GameState.WaveTransition)
        {
            currentAccessoryData = null;
            CurrentPhase = TransitionPhase.None;
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        latestCompletedWave = Mathf.Max(1, eventData.WaveNumber);
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
        upgradeCardOptions = Array.Empty<UpgradeCardSO>();
        upgradeOptionSnapshots = Array.Empty<UpgradeCardOptionSnapshot>();
        CurrentPhase = TransitionPhase.None;
        TryBindPlayerReferences();
        TryEnterNextPhase();
    }

    private void TryEnterNextPhase()
    {
        if (HasPendingChestSelection())
        {
            EnterChestSelection();
            return;
        }

        EnterUpgradeSelection();
    }

    private void EnterChestSelection()
    {
        CurrentPhase = TransitionPhase.ChestSelection;
        currentAccessoryData = ResourcesManager.GetRandomAccessory();
        GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
    }

    private void OnAccessoryOperated(AccessoryOperateEvent eventData)
    {
        if (CurrentPhase != TransitionPhase.ChestSelection)
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

        ConsumePendingChestSelection();
        currentAccessoryData = null;
        TryEnterNextPhase();
    }

    private void EnterUpgradeSelection()
    {
        currentAccessoryData = null;

        if (playerLevel == null || playerLevel.UnspentUpgradePoints <= 0)
        {
            CompleteUpgradeSelection();
            return;
        }

        CurrentPhase = TransitionPhase.UpgradeSelection;
        ConfigureUpgradeCards();
    }

    [NaughtyAttributes.Button("刷新升级卡片")]
    private void RefreshUpgradeCards()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WaveTransitionManager] Refresh upgrade cards can only run in Play Mode.", this);
            return;
        }

        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            Debug.LogWarning("[WaveTransitionManager] Upgrade cards can only be refreshed during UpgradeSelection phase.", this);
            return;
        }

        if (refreshUpgradeCardsRoutine != null)
        {
            return;
        }

        refreshUpgradeCardsRoutine = StartCoroutine(RefreshUpgradeCardsWithMotion());
    }

    private IEnumerator RefreshUpgradeCardsWithMotion()
    {
        WaveTransitionUpgradeCardGroup cardGroup = FindFirstObjectByType<WaveTransitionUpgradeCardGroup>();
        if (cardGroup != null)
        {
            yield return cardGroup.PlayRefreshOutAndWait();
        }

        ConfigureUpgradeCards();
        refreshUpgradeCardsRoutine = null;
    }

    private void ConfigureUpgradeCards()
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        UpgradeCardPoolSO pool = ResolveUpgradeCardPool();
        UpgradeCardOfferContext offerContext = new(
            upgradeRunState,
            latestCompletedWave,
            player != null ? player.GetComponent<WeaponsHolder>() : null);
        var rolledOptions = upgradeCardRollService.RollOptions(pool, offerContext);
        if (rolledOptions.Count == 0)
        {
            Debug.LogWarning("[WaveTransitionManager] No upgrade cards could be rolled. Completing upgrade selection.");
            CompleteUpgradeSelection();
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

    private void CompleteUpgradeSelection()
    {
        CurrentPhase = TransitionPhase.None;
        GameEventBus.Publish<UpgradeSelectionCompletedEvent>();
    }

    private void ContinueOrCompleteUpgradeSelection()
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        int remainingUpgradePoints = playerLevel.ConsumeUpgradePoint();
        if (remainingUpgradePoints > 0)
        {
            ConfigureUpgradeCards();
            return;
        }

        CompleteUpgradeSelection();
    }

    private void OnUpgradeContainerClicked(UpgradeContainerClickedEvent eventData)
    {
        if (CurrentPhase != TransitionPhase.UpgradeSelection)
        {
            return;
        }

        if (eventData.ContainerIndex < 0 || eventData.ContainerIndex >= upgradeCardOptions.Length)
        {
            Debug.LogWarning($"[WaveTransitionManager] Invalid upgrade card index {eventData.ContainerIndex}.");
            return;
        }

        UpgradeCardSO selectedCard = upgradeCardOptions[eventData.ContainerIndex];
        if (!upgradeCardApplyService.Apply(selectedCard, player))
        {
            Debug.LogWarning($"[WaveTransitionManager] Failed to apply upgrade card {selectedCard?.name}.");
            return;
        }

        upgradeRunState.RecordPick(selectedCard);
        ContinueOrCompleteUpgradeSelection();
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new WaveTransitionPhaseChangedEvent(TransitionPhase.None, CurrentPhase));

        switch (CurrentPhase)
        {
            case TransitionPhase.ChestSelection:
                if (currentAccessoryData != null)
                {
                    GameEventBus.Publish(new AccessorySelectionStartedEvent(currentAccessoryData));
                }

                break;
            case TransitionPhase.UpgradeSelection:
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

    private static bool HasPendingChestSelection()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null && gameManager.PendingChestSelectionCount > 0;
    }

    private static void ConsumePendingChestSelection()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager?.ConsumePendingChestSelection();
    }
}
