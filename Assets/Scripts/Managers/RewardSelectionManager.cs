using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

/// <summary>
/// 即时奖励选择流程控制器：负责奖励请求排队、暂停/恢复游戏模拟、驱动奖励选择 Popup，并在选择后应用玩法结果。
/// </summary>
public class RewardSelectionManager : MonoBehaviour
{
    private const string PAUSE_SOURCE_ID = "rewardSelection";
    private const string POPUP_GROUP_ID = "rewardSelection";

    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private Player player;
    [SerializeField] private ContentPoolSO upgradeCardPool;
    [SerializeField] private ContentPoolSO chestRewardPool;
    [SerializeField] private ContentPoolSO weaponRewardPool;

    private readonly Queue<RewardSelectionRequest> pendingRequests = new();
    private RewardSelectionOption[] currentOptions = Array.Empty<RewardSelectionOption>();
    private Dictionary<RewardSelectionReason, IRewardSelectionHandler> handlers;
    private ViewHandle<RewardSelectionPopup> currentPopupHandle;
    private GameManager gameManager;
    private CancellationTokenSource flowCancellation;
    private PlayerLevel playerLevel;
    private WeaponsHolder weaponsHolder;
    private int currentWaveNumber = 1;
    private RewardSelectionReason currentReason = RewardSelectionReason.None;
    private UniTaskCompletionSource<RewardSelectionResult> currentSelectionCompletionSource;
    private bool isProcessing;
    private bool pauseApplied;
    private bool upgradeRequestQueuedOrProcessing;

    private void OnEnable()
    {
        EnsureHandlers();
        GameEventBus.Subscribe<ChestCollectedEvent>(OnChestCollected);
        GameEventBus.Subscribe<UpgradeRewardAvailableEvent>(OnUpgradeRewardAvailable);
        GameEventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);

        TryBindSceneReferences();
        TryBindPlayerReferences();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<ChestCollectedEvent>(OnChestCollected);
        GameEventBus.Unsubscribe<UpgradeRewardAvailableEvent>(OnUpgradeRewardAvailable);
        GameEventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);

        ResetRewardFlow(resumeWave: false);
    }

    private void OnChestCollected()
    {
        EnqueueRequest(RewardSelectionReason.Chest);
    }

    private void OnUpgradeRewardAvailable(UpgradeRewardAvailableEvent eventData)
    {
        if (eventData.UnspentUpgradePoints <= 0 || upgradeRequestQueuedOrProcessing)
        {
            return;
        }

        upgradeRequestQueuedOrProcessing = true;
        EnqueueRequest(RewardSelectionReason.Upgrade);
    }

    private void EnqueueRequest(RewardSelectionReason reason)
    {
        if (reason == RewardSelectionReason.None || !IsGameStateActive())
        {
            return;
        }

        pendingRequests.Enqueue(new RewardSelectionRequest(reason));
        if (!isProcessing)
        {
            ProcessQueueAsync().Forget();
        }
    }

    private async UniTaskVoid ProcessQueueAsync()
    {
        if (isProcessing)
        {
            return;
        }

        isProcessing = true;
        flowCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken cancellationToken = flowCancellation.Token;
        ApplyRewardPause();

        try
        {
            while (pendingRequests.Count > 0 && IsGameStateActive())
            {
                RewardSelectionRequest request = pendingRequests.Dequeue();
                currentReason = request.Reason;

                switch (request.Reason)
                {
                    case RewardSelectionReason.Chest:
                    case RewardSelectionReason.Upgrade:
                    case RewardSelectionReason.Weapon:
                        await ProcessRequestAsync(request.Reason, cancellationToken);
                        if (request.Reason == RewardSelectionReason.Upgrade)
                        {
                            upgradeRequestQueuedOrProcessing = false;
                        }

                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            isProcessing = false;
            currentReason = RewardSelectionReason.None;
            currentOptions = Array.Empty<RewardSelectionOption>();
            upgradeRequestQueuedOrProcessing = ContainsUpgradeRequest();
            DisposeFlowCancellation();

            if (IsGameStateActive())
            {
                await CloseCurrentPopupAsync(CloseReason.Normal);
                ReleaseRewardPause(resumeWave: true);
            }
            else
            {
                await CloseCurrentPopupAsync(CloseReason.Cancel);
                ReleaseRewardPause(resumeWave: false);
            }

            if (pendingRequests.Count > 0 && IsGameStateActive())
            {
                ProcessQueueAsync().Forget();
            }
        }
    }

    private async UniTask ProcessRequestAsync(RewardSelectionReason reason, CancellationToken cancellationToken)
    {
        TryBindPlayerReferences();
        if (!TryGetHandler(reason, out IRewardSelectionHandler handler))
        {
            Debug.LogError($"[RewardSelectionManager] No handler registered for reward reason '{reason}'.", this);
            return;
        }

        bool hasProcessedSelection = false;
        while (IsGameStateActive())
        {
            RewardSelectionHandlerContext context = CreateHandlerContext();
            if (!handler.ShouldCreateSelection(context, hasProcessedSelection))
            {
                break;
            }

            RewardSelectionRound round = handler.CreateSelection(context);
            if (round == null || !round.HasAnyOption)
            {
                break;
            }

            RewardSelectionPopupModel model = new(
                round.Title,
                round.Description,
                round.CreateViewConfigs(),
                OnCurrentOptionSelected);
            UniTask<RewardSelectionResult> selectionTask = WaitForCurrentSelectionAsync(cancellationToken);
            await ShowOrRefreshPopupAsync(model, round.Options, cancellationToken);
            RewardSelectionResult selectionResult = await selectionTask;
            if (!TryResolveSelectedOption(selectionResult, out RewardSelectionOption selectedOption))
            {
                continue;
            }

            hasProcessedSelection = handler.ApplySelection(selectedOption, CreateHandlerContext()) || hasProcessedSelection;
        }

        await CloseCurrentPopupAsync(CloseReason.Normal);
    }

    private async UniTask ShowOrRefreshPopupAsync(
        RewardSelectionPopupModel model,
        RewardSelectionOption[] options,
        CancellationToken cancellationToken)
    {
        currentOptions = options ?? Array.Empty<RewardSelectionOption>();

        if (currentPopupHandle.IsValid && currentPopupHandle.View != null)
        {
            await currentPopupHandle.View.RefreshAsync(model, cancellationToken);
            return;
        }

        PopupOptions popupOptions = new PopupOptions(
            closeOnOutsideClick: false,
            groupId: POPUP_GROUP_ID,
            replaceSameGroup: true,
            trackInStack: false,
            preferredAnchor: FloatingViewAnchor.Center,
            showBackdrop: true);
        currentPopupHandle = await UIManager.Instance.ShowPopupAsync<RewardSelectionPopup>(
            model,
            popupOptions,
            cancellationToken);
    }

    private async UniTask<RewardSelectionResult> WaitForCurrentSelectionAsync(CancellationToken cancellationToken)
    {
        UniTaskCompletionSource<RewardSelectionResult> completionSource = new();
        currentSelectionCompletionSource = completionSource;
        CancellationTokenRegistration cancellationRegistration =
            cancellationToken.Register(() => completionSource.TrySetCanceled());
        try
        {
            return await completionSource.Task;
        }
        finally
        {
            cancellationRegistration.Dispose();
            if (ReferenceEquals(currentSelectionCompletionSource, completionSource))
            {
                currentSelectionCompletionSource = null;
            }
        }
    }

    private void OnCurrentOptionSelected(int optionIndex, string optionId)
    {
        RewardSelectionResult result = new RewardSelectionResult(optionIndex, optionId);
        if (!TryResolveSelectedOption(result, out _))
        {
            return;
        }

        currentSelectionCompletionSource?.TrySetResult(result);
    }

    private bool TryResolveSelectedOption(RewardSelectionResult result, out RewardSelectionOption selectedOption)
    {
        selectedOption = default;
        if (result.OptionIndex < 0 || result.OptionIndex >= currentOptions.Length)
        {
            return false;
        }

        RewardSelectionOption candidate = currentOptions[result.OptionIndex];
        if (!string.Equals(candidate.OptionId, result.OptionId, StringComparison.Ordinal))
        {
            return false;
        }

        selectedOption = candidate;
        return true;
    }

    private async UniTask CloseCurrentPopupAsync(CloseReason closeReason)
    {
        ViewHandle<RewardSelectionPopup> handle = currentPopupHandle;
        currentPopupHandle = default;
        if (!handle.IsValid)
        {
            return;
        }

        await handle.CloseAsync(closeReason);
    }

    private void ApplyRewardPause()
    {
        if (pauseApplied)
        {
            return;
        }

        TryBindSceneReferences();
        pauseApplied = true;
        gameManager?.RequestSimulationPause(PAUSE_SOURCE_ID);
        gameManager?.StopCurrentWave();
    }

    private void ReleaseRewardPause(bool resumeWave)
    {
        if (!pauseApplied)
        {
            return;
        }

        TryBindSceneReferences();
        pauseApplied = false;
        gameManager?.ReleaseSimulationPause(PAUSE_SOURCE_ID);
        if (resumeWave)
        {
            gameManager?.ResumeCurrentWave();
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        if (eventData.NewState == GameState.Game)
        {
            return;
        }

        ResetRewardFlow(resumeWave: false);
    }

    private void ResetRewardFlow(bool resumeWave)
    {
        pendingRequests.Clear();
        currentReason = RewardSelectionReason.None;
        currentOptions = Array.Empty<RewardSelectionOption>();
        isProcessing = false;
        upgradeRequestQueuedOrProcessing = false;
        DisposeFlowCancellation();
        CloseCurrentPopupAsync(CloseReason.Cancel).Forget();
        ReleaseRewardPause(resumeWave);
    }

    private void DisposeFlowCancellation()
    {
        if (flowCancellation == null)
        {
            return;
        }

        flowCancellation.Cancel();
        flowCancellation.Dispose();
        flowCancellation = null;
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
        accessoryManager = player != null ? player.GetComponent<AccessoryManager>() : null;
        playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
    }

    private void TryBindSceneReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
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

        if (weaponsHolder == null)
        {
            weaponsHolder = player.GetComponent<WeaponsHolder>();
        }
    }

    private bool IsGameStateActive()
    {
        TryBindSceneReferences();
        return gameManager != null && gameManager.CurrentGameState == GameState.Game;
    }

    private bool ContainsUpgradeRequest()
    {
        if (currentReason == RewardSelectionReason.Upgrade)
        {
            return true;
        }

        foreach (RewardSelectionRequest request in pendingRequests)
        {
            if (request.Reason == RewardSelectionReason.Upgrade)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureHandlers()
    {
        if (handlers != null)
        {
            return;
        }

        handlers = new Dictionary<RewardSelectionReason, IRewardSelectionHandler>();
        RegisterHandler(EquipmentRewardSelectionHandler.CreateAccessory());
        RegisterHandler(new UpgradeRewardSelectionHandler());
        RegisterHandler(EquipmentRewardSelectionHandler.CreateWeapon());
    }

    private void RegisterHandler(IRewardSelectionHandler handler)
    {
        if (handler == null || handler.Reason == RewardSelectionReason.None)
        {
            return;
        }

        handlers[handler.Reason] = handler;
    }

    private bool TryGetHandler(RewardSelectionReason reason, out IRewardSelectionHandler handler)
    {
        EnsureHandlers();
        return handlers.TryGetValue(reason, out handler);
    }

    private RewardSelectionHandlerContext CreateHandlerContext()
    {
        TryBindPlayerReferences();
        ResolveContentPoolsFromProvider();
        return new RewardSelectionHandlerContext(
            player,
            playerLevel,
            accessoryManager,
            weaponsHolder,
            RunContentHistoryRuntime.Current,
            currentWaveNumber,
            upgradeCardPool,
            chestRewardPool,
            weaponRewardPool,
            this);
    }

    private void ResolveContentPoolsFromProvider()
    {
        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            return;
        }

        if (upgradeCardPool == null)
        {
            upgradeCardPool = provider.UpgradeCardPool;
        }

        if (chestRewardPool == null)
        {
            chestRewardPool = provider.ChestRewardPool;
        }

        if (weaponRewardPool == null)
        {
            weaponRewardPool = provider.WeaponRewardPool;
        }
    }

    private readonly struct RewardSelectionRequest
    {
        public RewardSelectionRequest(RewardSelectionReason reason)
        {
            Reason = reason;
        }

        public RewardSelectionReason Reason { get; }
    }

}
