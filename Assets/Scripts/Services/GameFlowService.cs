using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Orange.GameServices;
using Orange.UIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using YokiFrame;

/// <summary>
/// 游戏主流程服务：负责全局游戏状态切换、暂停控制、回菜单以及主 UI 过渡编排。
/// </summary>
[Serializable]
public sealed class GameFlowService : GameService, IGameFlowController
{
    private const string STARTER_CARD_SELECTION_PAUSE_SOURCE_ID = "starterCardSelection";

    [SerializeField] private Player player;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameState initialGameState = GameState.Menu;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

    private IWaveController waveController;
    private IShopController shopController;
    private IEnemyRegistry enemyRegistry;
    private GameState currentGameState = GameState.None;
    private ViewHandle<GamePadUI> gamePadHandle;
    private Player moveInputPlayer;
    private IPlayerMoveInputReceiver moveInputReceiver;
    private CancellationTokenSource serviceCancellation;
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
    private bool isDisposed;

    public override GameServiceTickMode TickMode => GameServiceTickMode.Update;
    public GameState CurrentGameState => currentGameState;

    private CancellationToken ServiceToken => serviceCancellation != null ? serviceCancellation.Token : CancellationToken.None;
    private UnityEngine.Object LogContext => Context != null ? Context.Root : null;

    protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
    {
        dependencies.Require<IGameContentProvider>();
        dependencies.Require<IEnemyRegistry>();
        dependencies.Require<IWaveController>();
        dependencies.Require<IShopController>();
        dependencies.Optional<RunSummaryService>();
    }

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IGameFlowController>(this);
    }

    protected override void OnAttach()
    {
        isDisposed = false;
        serviceCancellation = new CancellationTokenSource();
        ResolveSceneReferences();

        YokiFrame.EventKit.Type.Register<WaveCompletedEvent>(OnWaveCompleted);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<WaveCompletedEvent>(OnWaveCompleted));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.MenuStartClicked, OnMenuStartClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.MenuStartClicked, OnMenuStartClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.ShopContinueClicked, OnShopContinueClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.ShopContinueClicked, OnShopContinueClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.GameOverRestartClicked, OnGameOverRestartClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.GameOverRestartClicked, OnGameOverRestartClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.GameOverReturnToMenuClicked, OnGameOverReturnToMenuClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.GameOverReturnToMenuClicked, OnGameOverReturnToMenuClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.StageCompleteRestartClicked, OnStageCompleteRestartClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.StageCompleteRestartClicked, OnStageCompleteRestartClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.StageCompleteReturnToMenuClicked, OnStageCompleteReturnToMenuClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.StageCompleteReturnToMenuClicked, OnStageCompleteReturnToMenuClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseRequested, OnPauseGameRequested);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseRequested, OnPauseGameRequested));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseMenuContinueClicked, OnPauseMenuContinueClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseMenuContinueClicked, OnPauseMenuContinueClicked));

        YokiFrame.EventKit.Enum.Register(GameFlowCommand.PauseMenuReturnToMenuClicked, OnPauseMenuReturnToMenuClicked);
        AddCleanup(() => YokiFrame.EventKit.Enum.UnRegister(GameFlowCommand.PauseMenuReturnToMenuClicked, OnPauseMenuReturnToMenuClicked));

        YokiFrame.EventKit.Type.Register<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged));

        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied));

        hasMoreWaves = waveController != null && waveController.HasMoreWaves;
    }

    protected override void OnStart()
    {
        Application.targetFrameRate = 60;
        TransitionToState(initialGameState);
        SetPaused(false);
    }

    protected override void OnUpdate(float deltaTime)
    {
        UpdateStandaloneMoveInput();
    }

    protected override void OnDispose()
    {
        isDisposed = true;
        stateTransitionVersion++;
        serviceCancellation?.Cancel();
        ClearPlayerMoveInput();
        DisposeServiceCancellation();
        Time.timeScale = 1f;
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
            CancellationToken cancellationToken = ServiceToken;
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
            Debug.LogException(exception, LogContext);
        }
    }

    private bool IsCurrentTransition(int transitionVersion)
    {
        return transitionVersion == stateTransitionVersion && !isDisposed;
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
            enemyRegistry?.ClearTracking();
            pauseSources.Clear();
        }
    }

    private void EnterState(GameState oldState, GameState newState)
    {
        if (newState == GameState.Game)
        {
            EnterGameState(oldState);
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
            await RunWaveEndPipelineAsync(ServiceToken);
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
            Debug.LogException(exception, LogContext);
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
        AttributeManager AttributeManager = player.GetComponent<AttributeManager>();
        if (currencyWallet == null || AttributeManager == null)
        {
            return;
        }

        int rewardAmount = PropValueUtility.FloatPointsToNonNegativeFlooredInt(
            AttributeManager.GetAttributeValue(PropType.WaveGoldRewardBonus));
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
            waveController != null ? waveController.CreateHudViewData() : default);
    }

    private StageCompletePageContext CreateStageCompletePageContext()
    {
        if (Context != null && Context.TryGet(out RunSummaryService runSummaryService))
        {
            return new StageCompletePageContext(runSummaryService.CreateResult());
        }

        if (GameServices.TryGet(out RunSummaryService globalRunSummaryService))
        {
            return new StageCompletePageContext(globalRunSummaryService.CreateResult());
        }

        throw new MissingReferenceException(
            $"{nameof(GameFlowService)} requires {nameof(RunSummaryService)} from {nameof(GameServices)}.");
    }

    private ShopPageContext CreateShopPageContext()
    {
        EnsurePlayerReference();
        ResolveShopController();
        if (shopController == null)
        {
            throw new MissingReferenceException($"{nameof(GameFlowService)} requires {nameof(IShopController)}.");
        }

        return new ShopPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<AttributeManager>(),
            shopController);
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

        ApplyStandaloneMoveInput(ReadGameplayMoveInput());
    }

    private static Vector2 ReadGameplayMoveInput()
    {
#if YOKIFRAME_INPUTSYSTEM_SUPPORT
        if (!InputKit.IsRegistered<SurvivorsInputActions>())
        {
            return Vector2.zero;
        }

        SurvivorsInputActions input = InputKit.Get<SurvivorsInputActions>();
        return input != default ? input.Gameplay.Move.ReadValue<Vector2>() : Vector2.zero;
#else
        return Vector2.zero;
#endif
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
                cancellationToken: ServiceToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, LogContext);
        }
    }

    private async UniTask ClosePauseMenuAndResumeAsync()
    {
        try
        {
            await ClosePageAsync<GamePauseMenu>(ServiceToken);
            SetPaused(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, LogContext);
        }
    }

    private async UniTask ClosePauseMenuAndReturnToMenuAsync()
    {
        try
        {
            await ClosePageAsync<GamePauseMenu>(ServiceToken);
            await ReturnToMenuAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, LogContext);
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
        ResolveWaveController();
        ResolveShopController();
        ResolveEnemyRegistry();

        if (mapGenerator == null)
        {
            mapGenerator = UnityEngine.Object.FindFirstObjectByType<MapGenerator>();
        }

        if (waveController == null)
        {
            throw new MissingReferenceException($"{nameof(GameFlowService)} requires {nameof(IWaveController)}.");
        }

        if (shopController == null)
        {
            throw new MissingReferenceException($"{nameof(GameFlowService)} requires {nameof(IShopController)}.");
        }

        if (enemyRegistry == null)
        {
            throw new MissingReferenceException($"{nameof(GameFlowService)} requires {nameof(IEnemyRegistry)}.");
        }
    }

    private void ResolveWaveController()
    {
        if (waveController != null)
        {
            return;
        }

        if (Context != null && Context.TryGet(out IWaveController resolvedWaveController))
        {
            waveController = resolvedWaveController;
        }
    }

    private void ResolveShopController()
    {
        if (shopController != null)
        {
            return;
        }

        if (Context != null && Context.TryGet(out IShopController resolvedShopController))
        {
            shopController = resolvedShopController;
        }
    }

    private void ResolveEnemyRegistry()
    {
        if (enemyRegistry != null)
        {
            return;
        }

        if (Context != null && Context.TryGet(out IEnemyRegistry resolvedEnemyRegistry))
        {
            enemyRegistry = resolvedEnemyRegistry;
        }
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            throw new MissingReferenceException(
                $"{nameof(GameFlowService)} requires an explicit {nameof(Player)} reference before opening gameplay UI.");
        }
    }

    private GamePauseMenuContext CreatePauseMenuContext()
    {
        EnsurePlayerReference();
        AttributeManager AttributeManager = player.GetComponent<AttributeManager>();
        if (AttributeManager == null)
        {
            throw new MissingReferenceException(
                $"{nameof(GameFlowService)} requires player '{player.name}' to have a {nameof(AttributeManager)} before opening the pause menu.");
        }

        return new GamePauseMenuContext(player, AttributeManager);
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
            Debug.LogWarning(
                $"{nameof(GameFlowService)} could not resolve starter cards because GameContentRuntime is unavailable.",
                LogContext);
            return;
        }

        StarterCardSelectionFlow flow = new StarterCardSelectionFlow(player, LogContext);
        await flow.RunAsync(provider.StarterCards, cancellationToken);
    }

    private void EnsureMapGenerated()
    {
        if (mapGenerator == null)
        {
            mapGenerator = UnityEngine.Object.FindFirstObjectByType<MapGenerator>();
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
            Debug.LogError($"{nameof(GameFlowService)}: 默认玩家 prefab 未在 GameContentCatalogSO 中配置，无法进入游戏。", LogContext);
            return;
        }

        player = UnityEngine.Object.Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
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
        ResolveWaveController();
        waveController?.StartFirstWave();
    }

    public void StartNextWave()
    {
        ResolveWaveController();
        waveController?.StartNextWave();
    }

    public void StopCurrentWave()
    {
        ResolveWaveController();
        waveController?.StopCurrentWave();
    }

    public void ResumeCurrentWave()
    {
        ResolveWaveController();
        waveController?.ResumeCurrentWave();
    }

    public void ResetWaves()
    {
        ResolveWaveController();
        waveController?.ResetWaves();
    }

    public void DefeatAllTrackedEnemies()
    {
        enemyRegistry?.DefeatAllTrackedEnemies();
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
                await CloseCurrentStatePageAsync(ServiceToken);
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
                Debug.LogException(exception, LogContext);
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

    private void DisposeServiceCancellation()
    {
        if (serviceCancellation == null)
        {
            return;
        }

        serviceCancellation.Dispose();
        serviceCancellation = null;
    }
}
