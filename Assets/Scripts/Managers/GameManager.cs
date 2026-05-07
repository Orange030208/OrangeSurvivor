using System;
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
public class GameManager : MonoSingletonBase<GameManager>
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Player player;
    [SerializeField] private CharacterSelectionManager characterSelectionManager;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private InventoryOperateManager inventoryOperateManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private StageCompleteSummaryManager stageCompleteSummaryManager;
    [SerializeField] private GameState initialGameState = GameState.Menu;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

    private GameState currentGameState = GameState.None;
    private bool isPaused;
    private bool hasMoreWaves;
    private int stateTransitionVersion;

    public GameState CurrentGameState => currentGameState;

    private void OnEnable()
    {
        ResolveSceneReferences();

        GameEventBus.Subscribe<WaveFlowDecisionEvent>(OnWaveFlowDecision);
        GameEventBus.Subscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Subscribe<CharacterSelectionCompletedEvent>(OnCharacterSelectionCompleted);
        GameEventBus.Subscribe<CharacterSelectionBackClickedEvent>(OnCharacterSelectionBackClicked);
        GameEventBus.Subscribe<MenuStartClickedEvent>(OnMenuStartClicked);
        GameEventBus.Subscribe<ShopContinueClickedEvent>(OnShopContinueClicked);
        GameEventBus.Subscribe<GameOverRestartClickedEvent>(OnGameOverRestartClicked);
        GameEventBus.Subscribe<GameOverReturnToMenuClickedEvent>(OnGameOverReturnToMenuClicked);
        GameEventBus.Subscribe<StageCompleteRestartClickedEvent>(OnStageCompleteRestartClicked);
        GameEventBus.Subscribe<StageCompleteReturnToMenuClickedEvent>(OnStageCompleteReturnToMenuClicked);
        GameEventBus.Subscribe<PauseGameRequestedEvent>(OnPauseGameRequested);
        GameEventBus.Subscribe<PauseMenuContinueClickedEvent>(OnPauseMenuContinueClicked);
        GameEventBus.Subscribe<PauseMenuReturnToMenuClickedEvent>(OnPauseMenuReturnToMenuClicked);
        GameEventBus.Subscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        GameEventBus.Publish(new RequestWaveRuntimeSnapshotEvent());
    }

    private void OnDisable()
    {
        stateTransitionVersion++;
        GameEventBus.Unsubscribe<WaveFlowDecisionEvent>(OnWaveFlowDecision);
        GameEventBus.Unsubscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Unsubscribe<CharacterSelectionCompletedEvent>(OnCharacterSelectionCompleted);
        GameEventBus.Unsubscribe<CharacterSelectionBackClickedEvent>(OnCharacterSelectionBackClicked);
        GameEventBus.Unsubscribe<MenuStartClickedEvent>(OnMenuStartClicked);
        GameEventBus.Unsubscribe<ShopContinueClickedEvent>(OnShopContinueClicked);
        GameEventBus.Unsubscribe<GameOverRestartClickedEvent>(OnGameOverRestartClicked);
        GameEventBus.Unsubscribe<GameOverReturnToMenuClickedEvent>(OnGameOverReturnToMenuClicked);
        GameEventBus.Unsubscribe<StageCompleteRestartClickedEvent>(OnStageCompleteRestartClicked);
        GameEventBus.Unsubscribe<StageCompleteReturnToMenuClickedEvent>(OnStageCompleteReturnToMenuClicked);
        GameEventBus.Unsubscribe<PauseGameRequestedEvent>(OnPauseGameRequested);
        GameEventBus.Unsubscribe<PauseMenuContinueClickedEvent>(OnPauseMenuContinueClicked);
        GameEventBus.Unsubscribe<PauseMenuReturnToMenuClickedEvent>(OnPauseMenuReturnToMenuClicked);
        GameEventBus.Unsubscribe<WaveRuntimeChangedEvent>(OnWaveRuntimeChanged);
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        TransitionToState(initialGameState);
        SetPaused(false);
    }

    private void OnWaveRuntimeChanged(WaveRuntimeChangedEvent eventData)
    {
        hasMoreWaves = eventData.HasMoreWaves;
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (eventData.Entity != player)
        {
            return;
        }

        TransitionToState(GameState.GameOver);
    }

    private void OnWaveFlowDecision(WaveFlowDecisionEvent eventData)
    {
        TransitionToState(eventData.NextState);
    }

    private void OnUpgradeSelectionCompleted()
    {
        if (currentGameState != GameState.WaveTransition)
        {
            return;
        }

        TransitionToState(GameState.Shop);
    }

    private void OnCharacterSelectionCompleted()
    {
        if (currentGameState != GameState.CharacterSelection)
        {
            return;
        }

        TransitionToState(GameState.Game);
    }

    private void OnCharacterSelectionBackClicked()
    {
        if (currentGameState != GameState.CharacterSelection)
        {
            return;
        }

        TransitionToState(GameState.Menu);
    }

    private void OnMenuStartClicked()
    {
        if (currentGameState != GameState.Menu)
        {
            return;
        }

        TransitionToState(GameState.CharacterSelection);
    }

    private void OnShopContinueClicked()
    {
        if (currentGameState != GameState.Shop)
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
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
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
        ExitState(oldState, targetState);
        currentGameState = targetState;
        ApplySimulationState();
        EnterState(oldState, currentGameState);
        GameEventBus.Publish(new GameStateChangedEvent(oldState, currentGameState));
    }

    private void OnGameOverRestartClicked()
    {
        RestartGame();
    }

    private void OnGameOverReturnToMenuClicked()
    {
        ReturnToMenu();
    }

    private void OnStageCompleteRestartClicked()
    {
        RestartGame();
    }

    private void OnStageCompleteReturnToMenuClicked()
    {
        ReturnToMenu();
    }

    private void OnPauseGameRequested()
    {
        if (currentGameState != GameState.Game || isPaused)
        {
            return;
        }

        SetPaused(true);
        OpenPauseMenu();
    }

    private bool ShouldBlockGameplayRequest(GameState targetState)
    {
        return targetState == GameState.Game
               && (currentGameState == GameState.Shop || currentGameState == GameState.WaveTransition)
               && !hasMoreWaves;
    }

    private void ExitState(GameState oldState, GameState newState)
    {
        if (oldState == GameState.Game && newState != GameState.Game)
        {
            GameEventBus.Publish(new StopCurrentWaveRequestedEvent());
        }

        if (newState == GameState.Shop || newState == GameState.WaveTransition)
        {
            GameEventBus.Publish(new DefeatAllEnemiesRequestedEvent());
        }

        if (newState == GameState.Menu || newState == GameState.GameOver || newState == GameState.StageComplete)
        {
            GameEventBus.Publish(new ResetWavesRequestedEvent());
            GameEventBus.Publish(new DefeatAllEnemiesRequestedEvent());
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

    private void EnterGameState(GameState oldState)
    {
        EnsureMapGenerated();
        EnsurePlayerSpawned();

        if (oldState == GameState.Shop || oldState == GameState.WaveTransition)
        {
            GameEventBus.Publish(new StartNextWaveRequestedEvent());
            return;
        }

        GameEventBus.Publish(new StartFirstWaveRequestedEvent());
    }

    private async UniTask CloseCurrentStatePageAsync(CancellationToken cancellationToken)
    {
        switch (currentGameState)
        {
            case GameState.Menu:
                await ClosePageAsync<MenuUIPage>(cancellationToken);
                break;
            case GameState.CharacterSelection:
                await ClosePageAsync<CharacterSelectUIPage>(cancellationToken);
                break;
            case GameState.Game:
                await ClosePageAsync<GamingUIPage>(cancellationToken);
                break;
            case GameState.GameOver:
                await ClosePageAsync<GameOverUIPage>(cancellationToken);
                break;
            case GameState.StageComplete:
                await ClosePageAsync<StageCompleteUIPage>(cancellationToken);
                break;
            case GameState.WaveTransition:
                await ClosePageAsync<WaveTransitionUIPage>(cancellationToken);
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
                await uiManager.OpenPageAsync<MenuUIPage>(cancellationToken: cancellationToken);
                break;
            case GameState.CharacterSelection:
                await uiManager.OpenPageAsync<CharacterSelectUIPage>(
                    characterSelectionManager,
                    cancellationToken);
                break;
            case GameState.Game:
                await uiManager.OpenPageAsync<GamingUIPage>(
                    CreateGamingPageContext(),
                    cancellationToken);
                break;
            case GameState.GameOver:
                await uiManager.OpenPageAsync<GameOverUIPage>(cancellationToken: cancellationToken);
                break;
            case GameState.StageComplete:
                await uiManager.OpenPageAsync<StageCompleteUIPage>(
                    CreateStageCompletePageContext(),
                    cancellationToken);
                break;
            case GameState.WaveTransition:
                await uiManager.OpenPageAsync<WaveTransitionUIPage>(cancellationToken: cancellationToken);
                break;
            case GameState.Shop:
                await uiManager.OpenPageAsync<ShopUIPage>(
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
            player.GetComponent<CurrencyWallet>());
    }

    private StageCompletePageContext CreateStageCompletePageContext()
    {
        if (stageCompleteSummaryManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(StageCompleteSummaryManager)} reference.");
        }

        return new StageCompletePageContext(stageCompleteSummaryManager.CreateSnapshot());
    }

    private ShopPageContext CreateShopPageContext()
    {
        EnsurePlayerReference();
        if (shopManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(ShopManager)} reference.");
        }

        if (inventoryOperateManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(InventoryOperateManager)} reference.");
        }

        inventoryOperateManager.Bind(player);
        return new ShopPageContext(
            player,
            player.GetComponent<CurrencyWallet>(),
            player.GetComponent<PropertiesManager>(),
            shopManager,
            inventoryOperateManager);
    }

    private void OpenPauseMenu()
    {
        OpenPauseMenuAsync().Forget();
    }

    private async UniTask OpenPauseMenuAsync()
    {
        if (uiManager.IsOpen<GamePauseMenu>())
        {
            return;
        }

        try
        {
            await uiManager.OpenPageAsync<GamePauseMenu>(
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
            ReturnToMenu();
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
        return uiManager.ClosePageAsync<TPage>(cancellationToken);
    }

    private void ResolveSceneReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(UIManager)} reference.");
        }

        if (characterSelectionManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(CharacterSelectionManager)} reference.");
        }
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(Player)} reference before opening gameplay UI.");
        }
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

        Player playerPrefab = ResourcesManager.GetDefaultPlayerPrefab();
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: 默认玩家 prefab 不存在，无法进入游戏。");
            return;
        }

        player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        GameEventBus.Publish(new PlayerSpawnedEvent(player));
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
        bool shouldRunSimulation = currentGameState == GameState.Game && !isPaused;
        Time.timeScale = shouldRunSimulation ? 1f : 0f;
    }

    private void ManageGameOver()
    {
        DOVirtual.DelayedCall(2f, RestartGame).SetUpdate(true);
    }

    private void RestartGame()
    {
        SetPaused(false);
        SceneManager.LoadScene(0);
    }

    private void ReturnToMenu()
    {
        SetPaused(false);
        TransitionToState(GameState.Menu);
        SceneManager.LoadScene(0);
    }
}

public enum GameState
{
    None,
    Menu,
    CharacterSelection,
    Game,
    GameOver,
    StageComplete,
    WaveTransition,
    Shop
}
