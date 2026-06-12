using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏主状态管理器：负责全局游戏状态切换、暂停控制、回菜单以及主 UI 过渡编排。
/// 它统一消费 UI 意图事件，并通过 UIManager 驱动页面切换。
/// </summary>
public class GameManager : MonoBehaviour
{
    private const string STARTER_CARD_SELECTION_PAUSE_SOURCE_ID = "starterCardSelection";

    [SerializeField] private Player player;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemyRegistry enemyRegistry;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private StageCompleteSummaryManager stageCompleteSummaryManager;
    [SerializeField] private GameState initialGameState = GameState.Menu;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

    private GameState currentGameState = GameState.None;
    private ViewHandle<GamePadUI> gamePadHandle;
    private Player moveInputPlayer;
    private IPlayerMoveInputReceiver moveInputReceiver;
    private bool gamePadControlsMoveInput;
    private bool isPaused;
    private bool hasMoreWaves;
    private bool isRunTerminated;
    private readonly HashSet<string> pauseSources = new();
    private int stateTransitionVersion;
    private bool isSceneReloading;
    private bool isWaveEndFlowRunning;
    private bool isEnteringPostWaveStateAfterCleanup;
    private bool hasShownStarterCardSelection;
    private bool shouldRunStarterCardSelectionAfterGamePageOpened;
    private bool shouldStartFirstWaveAfterGamePageOpened;

    public GameState CurrentGameState => currentGameState;

    private void OnEnable()
    {
        ResolveSceneReferences();

        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.MenuStartClicked, OnMenuStartClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.ShopContinueClicked, OnShopContinueClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.GameOverRestartClicked, OnGameOverRestartClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.GameOverReturnToMenuClicked, OnGameOverReturnToMenuClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.StageCompleteRestartClicked, OnStageCompleteRestartClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.StageCompleteReturnToMenuClicked, OnStageCompleteReturnToMenuClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseRequested, OnPauseGameRequested);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseMenuContinueClicked, OnPauseMenuContinueClicked);
        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseMenuReturnToMenuClicked, OnPauseMenuReturnToMenuClicked);
        YokiFrame.EventKit.Type.Register<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
        hasMoreWaves = waveManager != null && waveManager.HasMoreWaves;
    }

    private void OnDisable()
    {
        stateTransitionVersion++;
        YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.MenuStartClicked, OnMenuStartClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.ShopContinueClicked, OnShopContinueClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.GameOverRestartClicked, OnGameOverRestartClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.GameOverReturnToMenuClicked, OnGameOverReturnToMenuClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.StageCompleteRestartClicked, OnStageCompleteRestartClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.StageCompleteReturnToMenuClicked, OnStageCompleteReturnToMenuClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseRequested, OnPauseGameRequested);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseMenuContinueClicked, OnPauseMenuContinueClicked);
        YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseMenuReturnToMenuClicked, OnPauseMenuReturnToMenuClicked);
        YokiFrame.EventKit.Type.UnRegister<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        TransitionToState(initialGameState);
        SetPaused(false);
    }

    private void Update()
    {
        UpdateStandaloneMoveInput();
    }

    private void OnWaveRuntimeChanged(WaveRuntimeChangedEvent eventData)
    {
        hasMoreWaves = eventData.HasMoreWaves;
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (isRunTerminated || eventData.Entity != player)
        {
            return;
        }

        TerminateRunBecausePlayerDied();
        TransitionToState(GameState.GameOver);
    }

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        if (isRunTerminated || isWaveEndFlowRunning || currentGameState != GameState.Game)
        {
            return;
        }

        GrantWaveGoldRewardBonus();
        StartWaveEndFlowAsync(eventData).Forget();
    }

    private void OnMenuStartClicked()
    {
        if (currentGameState != GameState.Menu)
        {
            return;
        }

        TransitionToState(GameState.Game);
    }

    private void OnShopContinueClicked()
    {
        if (isRunTerminated || currentGameState != GameState.Shop)
        {
            return;
        }

        if (ShouldBlockGameplayRequest(GameState.Game))
        {
            return;
        }

        TransitionToState(GameState.Game);
    }

    private void OnPauseMenuContinueClicked()
    {
        if (!isPaused)
        {
            return;
        }

        ClosePauseMenuAndResumeAsync().Forget();
    }

    private void OnPauseMenuReturnToMenuClicked()
    {
        ClosePauseMenuAndReturnToMenuAsync().Forget();
    }

    private void TransitionToState(GameState targetState)
    {
        if (ShouldBlockTerminatedRunTransition(targetState))
        {
            return;
        }

        int transitionVersion = ++stateTransitionVersion;
        RunStateTransitionAsync(targetState, transitionVersion).Forget();
    }

    private async UniTask RunStateTransitionAsync(GameState targetState, int transitionVersion)
    {
        try
        {
            CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();
            await CloseCurrentStatePageAsync(cancellationToken);
            if (!IsCurrentTransition(transitionVersion))
            {
                return;
            }

            ApplyStateTransition(targetState);
            if (!IsCurrentTransition(transitionVersion))
            {
                return;
            }

            await OpenStatePageAsync(currentGameState, cancellationToken);
            if (!IsCurrentTransition(transitionVersion))
            {
                return;
            }

            await RunPostStatePageOpenedAsync(currentGameState, transitionVersion, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ResetStarterCardSelectionTransitionState();
        }
        catch (Exception exception)
        {
            ResetStarterCardSelectionTransitionState();
            Debug.LogException(exception, this);
        }
    }

    private bool IsCurrentTransition(int transitionVersion)
    {
        return transitionVersion == stateTransitionVersion && isActiveAndEnabled;
    }

    private void ApplyStateTransition(GameState targetState)
    {
        if (targetState == currentGameState)
        {
            return;
        }

        GameState oldState = currentGameState;
        bool wasEnteringPostWaveStateAfterCleanup = isEnteringPostWaveStateAfterCleanup;
        try
        {
            ExitState(oldState, targetState);
            currentGameState = targetState;
            ApplySimulationState();
            EnterState(oldState, currentGameState);
            ApplyStateMusic(currentGameState);
            YokiFrame.EventKit.Type.Send(new GameStateChangedEvent(oldState, currentGameState));
        }
        finally
        {
            if (wasEnteringPostWaveStateAfterCleanup)
            {
                isEnteringPostWaveStateAfterCleanup = false;
            }
        }
    }

    private void OnGameOverRestartClicked()
    {
        RestartGameAsync().Forget();
    }

    private void OnGameOverReturnToMenuClicked()
    {
        ReturnToMenuAsync().Forget();
    }

    private void OnStageCompleteRestartClicked()
    {
        RestartGameAsync().Forget();
    }

    private void OnStageCompleteReturnToMenuClicked()
    {
        ReturnToMenuAsync().Forget();
    }

    private void OnPauseGameRequested()
    {
        if (currentGameState != GameState.Game || isPaused || isWaveEndFlowRunning)
        {
            return;
        }

        SetPaused(true);
        OpenPauseMenu();
    }

    private bool ShouldBlockGameplayRequest(GameState targetState)
    {
        if (ShouldBlockTerminatedRunTransition(targetState))
        {
            return true;
        }

        return targetState == GameState.Game
               && currentGameState == GameState.Shop
               && !hasMoreWaves;
    }

    private bool ShouldBlockTerminatedRunTransition(GameState targetState)
    {
        return isRunTerminated && IsRunContinuingState(targetState);
    }

    private static bool IsRunContinuingState(GameState state)
    {
        return state == GameState.Game || state == GameState.Shop;
    }

    private void TerminateRunBecausePlayerDied()
    {
        // UI 页面切换是异步的，死亡时先锁定本局，避免延迟到达的波次/商店事件覆盖 GameOver。
        isRunTerminated = true;
        StopCurrentWave();
    }

    private void ExitState(GameState oldState, GameState newState)
    {
        if (oldState == GameState.Game && newState != GameState.Game)
        {
            StopCurrentWave();
        }

        if (newState != GameState.Game)
        {
            shouldRunStarterCardSelectionAfterGamePageOpened = false;
            shouldStartFirstWaveAfterGamePageOpened = false;
            ReleaseSimulationPause(STARTER_CARD_SELECTION_PAUSE_SOURCE_ID);
        }

        if (newState == GameState.Shop && !isEnteringPostWaveStateAfterCleanup)
        {
            DefeatAllTrackedEnemies();
        }

        if (newState == GameState.Menu || newState == GameState.GameOver || newState == GameState.StageComplete)
        {
            ResetWaves();
            DefeatAllTrackedEnemies();
            if (enemyRegistry != null)
            {
                enemyRegistry.ClearTracking();
            }
            pauseSources.Clear();
        }
    }

    private void EnterState(GameState oldState, GameState newState)
    {
        if (newState == GameState.Game)
        {
            EnterGameState(oldState);
            return;
        }

        if (newState == GameState.GameOver)
        {
            return;
        }

        if (newState == GameState.StageComplete)
        {
            return;
        }
    }

    private void ApplyStateMusic(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.Menu);
                break;
            case GameState.Game:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.Gameplay);
                break;
            case GameState.GameOver:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.GameOver);
                break;
            case GameState.StageComplete:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.StageComplete);
                break;
            case GameState.Shop:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.Shop);
                break;
            default:
                AudioPlaybackBridge.RequestStopMusic();
                break;
        }
    }

    private void EnterGameState(GameState oldState)
    {
        RestorePlayerAfterWaveCleanup();
        if (oldState != GameState.Shop)
        {
            isRunTerminated = false;
        }

        EnsureMapGenerated();
        EnsurePlayerSpawned();

        if (oldState == GameState.Shop)
        {
            StartNextWave();
            return;
        }

        if (!hasShownStarterCardSelection)
        {
            hasShownStarterCardSelection = true;
            shouldRunStarterCardSelectionAfterGamePageOpened = true;
            return;
        }

        shouldStartFirstWaveAfterGamePageOpened = true;
    }

    private async UniTaskVoid StartWaveEndFlowAsync(WaveCompletedEvent completedEvent)
    {
        if (isRunTerminated || isWaveEndFlowRunning)
        {
            return;
        }

        isWaveEndFlowRunning = true;
        bool transitionRequested = false;
        try
        {
            await RunWaveEndPipelineAsync(this.GetCancellationTokenOnDestroy());
            if (isRunTerminated || currentGameState != GameState.Game)
            {
                return;
            }

            isEnteringPostWaveStateAfterCleanup = true;
            if (!completedEvent.HasNextWave)
            {
                YokiFrame.EventKit.Enum.Send(WaveMilestone.AllWavesCompleted);
                AudioSfxBridge.RequestPlay(AudioSfxKey.StageCompleted);
                transitionRequested = true;
                TransitionToState(GameState.StageComplete);
                return;
            }

            transitionRequested = true;
            TransitionToState(GameState.Shop);
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
            if (!transitionRequested)
            {
                RestorePlayerAfterWaveCleanup();
            }

            isWaveEndFlowRunning = false;
        }
    }

    private async UniTask RunWaveEndPipelineAsync(CancellationToken cancellationToken)
    {
        WaveEndPipeline pipeline = WaveEndPipelineFactory.CreateDefault(player, enemyRegistry);
        await pipeline.RunAsync(cancellationToken);
    }

    private void RestorePlayerAfterWaveCleanup()
    {
        if (player == null)
        {
            return;
        }

        if (player.MoveComponent is IMovementLockable movementLockable)
        {
            movementLockable.RemoveMovementLock(typeof(WaveEndPipeline));
        }

        if (player.TryGetComponent(out WeaponsHolder weaponsHolder))
        {
            weaponsHolder.EnableWeaponsAfterWaveCleanup();
        }
    }

    private void GrantWaveGoldRewardBonus()
    {
        if (player == null)
        {
            return;
        }

        CurrencyWallet currencyWallet = player.GetComponent<CurrencyWallet>();
        PropertiesManager propertiesManager = player.GetComponent<PropertiesManager>();
        if (currencyWallet == null || propertiesManager == null)
        {
            return;
        }

        int rewardAmount = PropValueUtility.FloatPointsToNonNegativeFlooredInt(
            propertiesManager.GetPropValue(PropType.WaveGoldRewardBonus));
        if (rewardAmount <= 0)
        {
            return;
        }

        currencyWallet.ChangeAmount(rewardAmount);
    }

    private async UniTask CloseCurrentStatePageAsync(CancellationToken cancellationToken)
    {
        switch (currentGameState)
        {
            case GameState.Menu:
                await ClosePageAsync<MenuUIPage>(cancellationToken);
                break;
            case GameState.Game:
                ClearPlayerMoveInput();
                gamePadControlsMoveInput = false;
                await CloseGamePadAsync(cancellationToken);
                await ClosePageAsync<GamingUIPage>(cancellationToken);
                break;
            case GameState.GameOver:
                await ClosePageAsync<GameOverUIPage>(cancellationToken);
                break;
            case GameState.StageComplete:
                await ClosePageAsync<StageCompleteUIPage>(cancellationToken);
                break;
            case GameState.Shop:
                await ClosePageAsync<ShopUIPage>(cancellationToken);
                break;
        }
    }

    private async UniTask OpenStatePageAsync(GameState state, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case GameState.Menu:
                await UIManager.Instance.OpenPageAsync<MenuUIPage>(cancellationToken: cancellationToken);
                break;
            case GameState.Game:
                await UIManager.Instance.OpenPageAsync<GamingUIPage>(
                    CreateGamingPageContext(),
                    cancellationToken);
                await OpenGamePadAsync(cancellationToken);
                break;
            case GameState.GameOver:
                await UIManager.Instance.OpenPageAsync<GameOverUIPage>(cancellationToken: cancellationToken);
                break;
            case GameState.StageComplete:
                await UIManager.Instance.OpenPageAsync<StageCompleteUIPage>(
                    CreateStageCompletePageContext(),
                    cancellationToken);
                break;
            case GameState.Shop:
                await UIManager.Instance.OpenPageAsync<ShopUIPage>(
                    CreateShopPageContext(),
                    cancellationToken);
                break;
        }
    }

    private GamingPageContext CreateGamingPageContext()
    {
        EnsurePlayerReference();
        return new GamingPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            waveManager != null ? waveManager.CreateHudViewData() : default);
    }

    private StageCompletePageContext CreateStageCompletePageContext()
    {
        if (stageCompleteSummaryManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(StageCompleteSummaryManager)} reference.");
        }

        return new StageCompletePageContext(stageCompleteSummaryManager.CreateResult());
    }

    private ShopPageContext CreateShopPageContext()
    {
        EnsurePlayerReference();
        if (shopManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(ShopManager)} reference.");
        }

        return new ShopPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<PropertiesManager>(),
            shopManager);
    }

    private async UniTask OpenGamePadAsync(CancellationToken cancellationToken)
    {
        if (gamePadHandle.IsValid)
        {
            gamePadControlsMoveInput = true;
            return;
        }

        if (!GamePadUI.IsRegisteredTouchControlsEnabled(UIManager.Instance.Catalog, Application.platform))
        {
            gamePadControlsMoveInput = false;
            return;
        }

        gamePadHandle = await UIManager.Instance.ShowPopupAsync<GamePadUI>(
            new GamePadUIContext(player),
            CreateGamePadPopupOptions(),
            cancellationToken);
        gamePadControlsMoveInput = true;
    }

    private async UniTask CloseGamePadAsync(CancellationToken cancellationToken)
    {
        ViewHandle<GamePadUI> handle = gamePadHandle;
        if (!handle.IsValid)
        {
            return;
        }

        gamePadHandle = default;
        await handle.CloseAsync(CloseReason.Normal, cancellationToken);
    }

    private void UpdateStandaloneMoveInput()
    {
        if (currentGameState != GameState.Game || gamePadControlsMoveInput)
        {
            return;
        }

        GameInput input = GameInput.Instance;
        Vector2 moveInput = input != null ? input.Move : Vector2.zero;
        ApplyStandaloneMoveInput(moveInput);
    }

    private void ApplyStandaloneMoveInput(Vector2 moveInput)
    {
        IPlayerMoveInputReceiver receiver = ResolveMoveInputReceiver();
        if (receiver == null)
        {
            return;
        }

        receiver.SetMoveInput(Vector2.ClampMagnitude(moveInput, 1f));
    }

    private void ClearPlayerMoveInput()
    {
        ResolveMoveInputReceiver()?.SetMoveInput(Vector2.zero);
    }

    private IPlayerMoveInputReceiver ResolveMoveInputReceiver()
    {
        if (player == null)
        {
            moveInputPlayer = null;
            moveInputReceiver = null;
            return null;
        }

        if (moveInputPlayer == player)
        {
            return moveInputReceiver;
        }

        moveInputPlayer = player;
        moveInputReceiver = player.GetComponent<IPlayerMoveInputReceiver>();
        return moveInputReceiver;
    }

    private void OpenPauseMenu()
    {
        OpenPauseMenuAsync().Forget();
    }

    private async UniTask OpenPauseMenuAsync()
    {
        if (UIManager.Instance.IsOpen<GamePauseMenu>())
        {
            return;
        }

        try
        {
            await UIManager.Instance.OpenPageAsync<GamePauseMenu>(
                CreatePauseMenuContext(),
                cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private async UniTask ClosePauseMenuAndResumeAsync()
    {
        try
        {
            await ClosePageAsync<GamePauseMenu>(this.GetCancellationTokenOnDestroy());
            SetPaused(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private async UniTask ClosePauseMenuAndReturnToMenuAsync()
    {
        try
        {
            await ClosePageAsync<GamePauseMenu>(this.GetCancellationTokenOnDestroy());
            await ReturnToMenuAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private UniTask<bool> ClosePageAsync<TPage>(CancellationToken cancellationToken)
        where TPage : PageBase
    {
        return UIManager.Instance.ClosePageAsync<TPage>(cancellationToken);
    }

    private static PopupOptions CreateGamePadPopupOptions()
    {
        return new PopupOptions(
            closeOnOutsideClick: false,
            showBackdrop: false,
            trackInStack: false,
            groupId: "gamepad",
            replaceSameGroup: false);
    }

    private void ResolveSceneReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (enemyRegistry == null)
        {
            enemyRegistry = FindFirstObjectByType<EnemyRegistry>();
        }

        if (waveManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(WaveManager)} reference.");
        }

        if (enemyRegistry == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(EnemyRegistry)} reference.");
        }
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(Player)} reference before opening gameplay UI.");
        }
    }

    private GamePauseMenuContext CreatePauseMenuContext()
    {
        EnsurePlayerReference();
        PropertiesManager propertiesManager = player.GetComponent<PropertiesManager>();
        if (propertiesManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires player '{player.name}' to have a {nameof(PropertiesManager)} before opening the pause menu.");
        }

        return new GamePauseMenuContext(player, propertiesManager);
    }

    private async UniTask RunPostStatePageOpenedAsync(
        GameState openedState,
        int transitionVersion,
        CancellationToken cancellationToken)
    {
        if (openedState != GameState.Game)
        {
            return;
        }

        if (shouldRunStarterCardSelectionAfterGamePageOpened)
        {
            shouldRunStarterCardSelectionAfterGamePageOpened = false;
            RequestSimulationPause(STARTER_CARD_SELECTION_PAUSE_SOURCE_ID);
            try
            {
                await RunStarterCardSelectionAsync(cancellationToken);
            }
            finally
            {
                ReleaseSimulationPause(STARTER_CARD_SELECTION_PAUSE_SOURCE_ID);
            }

            if (IsCurrentTransition(transitionVersion) && currentGameState == GameState.Game)
            {
                StartFirstWave();
            }

            return;
        }

        if (shouldStartFirstWaveAfterGamePageOpened)
        {
            shouldStartFirstWaveAfterGamePageOpened = false;
            StartFirstWave();
        }
    }

    private void ResetStarterCardSelectionTransitionState()
    {
        shouldRunStarterCardSelectionAfterGamePageOpened = false;
        shouldStartFirstWaveAfterGamePageOpened = false;
        ReleaseSimulationPause(STARTER_CARD_SELECTION_PAUSE_SOURCE_ID);
    }

    private async UniTask RunStarterCardSelectionAsync(CancellationToken cancellationToken)
    {
        if (!GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            Debug.LogWarning($"{nameof(GameManager)} could not resolve starter cards because GameContentRuntime is unavailable.", this);
            return;
        }

        StarterCardSelectionFlow flow = new StarterCardSelectionFlow(player, this);
        await flow.RunAsync(provider.StarterCards, cancellationToken);
    }

    private void EnsureMapGenerated()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (mapGenerator == null)
        {
            return;
        }

        mapGenerator.GenerateIfNeeded();
    }

    private void EnsurePlayerSpawned()
    {
        if (player != null)
        {
            return;
        }

        Player playerPrefab = GameContentRuntime.Provider.DefaultPlayerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: 默认玩家 prefab 未在 GameContentCatalogSO 中配置，无法进入游戏。");
            return;
        }

        player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        YokiFrame.EventKit.Type.Send(new PlayerSpawnedEvent(player));
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;
        ApplySimulationState();
    }

    private void ApplySimulationState()
    {
        bool shouldRunSimulation = currentGameState == GameState.Game
                                   && !isPaused
                                   && pauseSources.Count == 0;
        Time.timeScale = shouldRunSimulation ? 1f : 0f;
    }

    public void RequestSimulationPause(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (pauseSources.Add(sourceId))
        {
            ApplySimulationState();
        }
    }

    public void ReleaseSimulationPause(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (pauseSources.Remove(sourceId))
        {
            ApplySimulationState();
        }
    }

    public void StartFirstWave()
    {
        waveManager?.StartFirstWave();
    }

    public void StartNextWave()
    {
        waveManager?.StartNextWave();
    }

    public void StopCurrentWave()
    {
        waveManager?.StopCurrentWave();
    }

    public void ResumeCurrentWave()
    {
        waveManager?.ResumeCurrentWave();
    }

    public void ResetWaves()
    {
        waveManager?.ResetWaves();
    }

    public void DefeatAllTrackedEnemies()
    {
        enemyRegistry?.DefeatAllTrackedEnemies();
    }

    private void ManageGameOver()
    {
        DOVirtual.DelayedCall(2f, () => RestartGameAsync().Forget()).SetUpdate(true);
    }

    private async UniTask RestartGameAsync()
    {
        await ReloadCurrentSceneAsync();
    }

    private async UniTask ReturnToMenuAsync()
    {
        await ReloadCurrentSceneAsync();
    }

    private async UniTask ReloadCurrentSceneAsync()
    {
        if (isSceneReloading)
        {
            return;
        }

        isSceneReloading = true;
        try
        {
            int reloadVersion = ++stateTransitionVersion;

            try
            {
                await CloseCurrentStatePageAsync(this.GetCancellationTokenOnDestroy());
                if (!IsCurrentTransition(reloadVersion))
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            pauseSources.Clear();
            isPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        finally
        {
            isSceneReloading = false;
        }
    }
}

public enum GameState
{
    None,
    Menu,
    Game,
    GameOver,
    StageComplete,
    Shop
}
