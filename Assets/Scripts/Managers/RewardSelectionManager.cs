using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.UIFramework;
using UnityEngine;

/// <summary>
/// 即时奖励选择流程控制器：负责奖励请求排队、暂停/恢复游戏模拟、驱动三选一 Popup，并在选择后应用玩法结果。
/// </summary>
public class RewardSelectionManager : MonoBehaviour
{
    private const string PAUSE_SOURCE_ID = "rewardSelection";
    private const string POPUP_GROUP_ID = "rewardSelection";
    private const int OPTION_COUNT = 3;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private Player player;
    [SerializeField] private UpgradeCardPoolSO upgradeCardPool;

    private readonly Queue<RewardSelectionRequest> pendingRequests = new();
    private readonly UpgradeRunState upgradeRunState = new();
    private readonly UpgradeCardRollService upgradeCardRollService = new();
    private readonly UpgradeCardApplyService upgradeCardApplyService = new();
    private RewardSelectionOption[] currentOptions = Array.Empty<RewardSelectionOption>();
    private ViewHandle<RewardSelectionPopup> currentPopupHandle;
    private GameManager gameManager;
    private CancellationTokenSource flowCancellation;
    private PlayerLevel playerLevel;
    private int currentWaveNumber = 1;
    private RewardSelectionReason currentReason = RewardSelectionReason.None;
    private UniTaskCompletionSource<RewardSelectionResult> currentSelectionCompletionSource;
    private bool isProcessing;
    private bool pauseApplied;
    private bool upgradeRequestQueuedOrProcessing;

    private void OnEnable()
    {
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
                        await ProcessChestRequestAsync(cancellationToken);
                        break;
                    case RewardSelectionReason.Upgrade:
                        await ProcessUpgradeRequestAsync(cancellationToken);
                        upgradeRequestQueuedOrProcessing = false;
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

    private async UniTask ProcessChestRequestAsync(CancellationToken cancellationToken)
    {
        TryBindPlayerReferences();
        AccessoryDataSO[] accessories = ResourcesManager.GetRandomAccessories(OPTION_COUNT);
        if (accessories.Length == 0)
        {
            Debug.LogWarning("[RewardSelectionManager] No accessories could be rolled for chest reward.", this);
            return;
        }

        UniTask<RewardSelectionResult> selectionTask = WaitForCurrentSelectionAsync(cancellationToken);
        RewardSelectionPopupModel model = CreateChestPopupModel(accessories, out RewardSelectionOption[] options);
        await ShowOrRefreshPopupAsync(model, options, cancellationToken);
        RewardSelectionResult selectionResult = await selectionTask;
        if (!TryResolveSelectedOption(selectionResult, out RewardSelectionOption selectedOption))
        {
            return;
        }

        if (selectedOption.AccessoryData != null)
        {
            accessoryManager?.EquipAccessory(selectedOption.AccessoryData);
        }

        await CloseCurrentPopupAsync(CloseReason.Normal);
    }

    private async UniTask ProcessUpgradeRequestAsync(CancellationToken cancellationToken)
    {
        TryBindPlayerReferences();
        while (playerLevel != null && playerLevel.UnspentUpgradePoints > 0 && IsGameStateActive())
        {
            List<UpgradeCardSO> rolledCards = RollUpgradeCards();
            if (rolledCards.Count == 0)
            {
                Debug.LogWarning("[RewardSelectionManager] No upgrade cards could be rolled for upgrade reward.", this);
                return;
            }

            UniTask<RewardSelectionResult> selectionTask = WaitForCurrentSelectionAsync(cancellationToken);
            RewardSelectionPopupModel model = CreateUpgradePopupModel(rolledCards, out RewardSelectionOption[] options);
            await ShowOrRefreshPopupAsync(model, options, cancellationToken);
            RewardSelectionResult selectionResult = await selectionTask;
            if (!TryResolveSelectedOption(selectionResult, out RewardSelectionOption selectedOption))
            {
                continue;
            }

            UpgradeCardSO selectedCard = selectedOption.UpgradeCard;
            if (!upgradeCardApplyService.Apply(selectedCard, player))
            {
                Debug.LogWarning($"[RewardSelectionManager] Failed to apply upgrade card {selectedCard?.name}.", this);
                continue;
            }

            upgradeRunState.RecordPick(selectedCard);
            playerLevel.ConsumeUpgradePoint();
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
            preferredAnchor: FloatingViewAnchor.Center);
        currentPopupHandle = await ResolveUIManager().ShowPopupAsync<RewardSelectionPopup>(
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

    private RewardSelectionPopupModel CreateChestPopupModel(
        AccessoryDataSO[] accessories,
        out RewardSelectionOption[] options)
    {
        int count = Mathf.Min(OPTION_COUNT, accessories.Length);
        RewardSelectionCardViewModel[] cardModels = new RewardSelectionCardViewModel[count];
        options = new RewardSelectionOption[count];

        for (int i = 0; i < count; i++)
        {
            AccessoryDataSO accessory = accessories[i];
            string optionId = accessory != null ? accessory.AccessoryId : string.Empty;
            cardModels[i] = new RewardSelectionCardViewModel(
                optionId,
                accessory != null ? accessory.ItemName : string.Empty,
                accessory != null ? accessory.ItemIcon : null,
                accessory != null ? accessory.Description : string.Empty,
                accessory != null ? CardQualityResolver.FromAccessoryRarity(accessory.RarityGrade) : CardQuality.Common,
                new[] { "饰品" },
                accessory != null);
            options[i] = RewardSelectionOption.ForAccessory(optionId, accessory);
        }

        return new RewardSelectionPopupModel("选择宝箱奖励", "选择 1 个饰品立即装备。", cardModels, OnCurrentOptionSelected);
    }

    private RewardSelectionPopupModel CreateUpgradePopupModel(
        IReadOnlyList<UpgradeCardSO> cards,
        out RewardSelectionOption[] options)
    {
        int count = Mathf.Min(OPTION_COUNT, cards.Count);
        RewardSelectionCardViewModel[] cardModels = new RewardSelectionCardViewModel[count];
        options = new RewardSelectionOption[count];

        for (int i = 0; i < count; i++)
        {
            UpgradeCardSO card = cards[i];
            UpgradeCardOptionViewData viewData = card.CreateOptionViewData(upgradeRunState);
            cardModels[i] = new RewardSelectionCardViewModel(
                viewData.CardId,
                viewData.Title,
                viewData.Icon,
                viewData.Description,
                CardQualityResolver.FromUpgradeCardRarity(viewData.Rarity),
                BuildUpgradeTagLabels(viewData.Tags),
                card != null);
            options[i] = RewardSelectionOption.ForUpgradeCard(viewData.CardId, card);
        }

        return new RewardSelectionPopupModel("选择升级奖励", "选择 1 张升级卡。", cardModels, OnCurrentOptionSelected);
    }

    private List<UpgradeCardSO> RollUpgradeCards()
    {
        UpgradeCardPoolSO pool = ResolveUpgradeCardPool();
        UpgradeCardOfferContext offerContext = new(
            upgradeRunState,
            currentWaveNumber,
            player != null ? player.GetComponent<WeaponsHolder>() : null);
        return upgradeCardRollService.RollOptions(pool, offerContext);
    }

    private UpgradeCardPoolSO ResolveUpgradeCardPool()
    {
        if (upgradeCardPool == null)
        {
            upgradeCardPool = ResourcesManager.GetUpgradeCardPool();
        }

        return upgradeCardPool;
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
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }

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
    }

    private UIManager ResolveUIManager()
    {
        TryBindSceneReferences();
        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(RewardSelectionManager)} requires an active {nameof(UIManager)}.");
        }

        return uiManager;
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

    private static string[] BuildUpgradeTagLabels(UpgradeCardTag[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return Array.Empty<string>();
        }

        string[] labels = new string[tags.Length];
        for (int i = 0; i < tags.Length; i++)
        {
            labels[i] = ItemDescriptionUtility.FormatUpgradeCardTag(tags[i]);
        }

        return labels;
    }

    private readonly struct RewardSelectionRequest
    {
        public RewardSelectionRequest(RewardSelectionReason reason)
        {
            Reason = reason;
        }

        public RewardSelectionReason Reason { get; }
    }

    private readonly struct RewardSelectionOption
    {
        private RewardSelectionOption(string optionId, UpgradeCardSO upgradeCard, AccessoryDataSO accessoryData)
        {
            OptionId = optionId ?? string.Empty;
            UpgradeCard = upgradeCard;
            AccessoryData = accessoryData;
        }

        public string OptionId { get; }
        public UpgradeCardSO UpgradeCard { get; }
        public AccessoryDataSO AccessoryData { get; }

        public static RewardSelectionOption ForUpgradeCard(string optionId, UpgradeCardSO upgradeCard)
        {
            return new RewardSelectionOption(optionId, upgradeCard, null);
        }

        public static RewardSelectionOption ForAccessory(string optionId, AccessoryDataSO accessoryData)
        {
            return new RewardSelectionOption(optionId, null, accessoryData);
        }
    }
}
