using DG.Tweening;
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
    [SerializeField] private GameState initialGameState = GameState.Menu;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

    private GameState currentGameState = GameState.None;
    private bool isPaused;
    private bool hasMoreWaves;

    public GameState CurrentGameState => currentGameState;
    public bool IsSimulationRunning => currentGameState == GameState.Game && !isPaused;

    private void OnEnable()
    {
        uiManager = FindFirstObjectByType<UIManager>();

        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Subscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Subscribe<CharacterSelectionCompletedEvent>(OnCharacterSelectionCompleted);
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
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Unsubscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Unsubscribe<CharacterSelectionCompletedEvent>(OnCharacterSelectionCompleted);
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

    private void OnWaveCompleted(WaveCompletedEvent _)
    {
        TransitionToState(GetNextStateAfterWaveCompleted());
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

        uiManager.BeginTransition()
            .ClosePage<GamePauseMenu>()
            .Callback(() => SetPaused(false))
            .Play();
    }

    private void OnPauseMenuReturnToMenuClicked()
    {
        uiManager.BeginTransition()
            .ClosePage<GamePauseMenu>()
            .Callback(ReturnToMenu)
            .Play();
    }

    private void TransitionToState(GameState targetState)
    {
        IUITransitionSequence transition = uiManager.BeginTransition();
        AppendCloseCurrentStatePage(transition);
        transition
            .Callback(() => ApplyStateTransition(targetState))
            .Callback(OpenStatePage)
            .Play();
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

    private GameState GetNextStateAfterWaveCompleted()
    {
        if (!hasMoreWaves)
        {
            GameEventBus.Publish<AllWavesCompletedEvent>();
            return GameState.StageComplete;
        }

        if (player.IsLevelUpInCurrentWave)
        {
            return GameState.WaveTransition;
        }

        return GameState.Shop;
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
        EnsurePlayerSpawned();

        if (oldState == GameState.Shop || oldState == GameState.WaveTransition)
        {
            GameEventBus.Publish(new StartNextWaveRequestedEvent());
            return;
        }

        GameEventBus.Publish(new StartFirstWaveRequestedEvent());
    }

    private void AppendCloseCurrentStatePage(IUITransitionSequence transition)
    {
        switch (currentGameState)
        {
            case GameState.Menu:
                transition.ClosePage<MenuUIPage>();
                break;
            case GameState.CharacterSelection:
                transition.ClosePage<CharacterSelectUIPage>();
                break;
            case GameState.Game:
                transition.ClosePage<GamingUIPage>();
                break;
            case GameState.GameOver:
                transition.ClosePage<GameOverUIPage>();
                break;
            case GameState.StageComplete:
                transition.ClosePage<StageCompleteUIPage>();
                break;
            case GameState.WaveTransition:
                transition.ClosePage<WaveTransitionUIPage>();
                break;
            case GameState.Shop:
                transition.ClosePage<ShopUIPage>();
                break;
        }
    }

    private void OpenStatePage()
    {
        switch (currentGameState)
        {
            case GameState.Menu:
                uiManager.OpenPage<MenuUIPage>();
                break;
            case GameState.CharacterSelection:
                uiManager.OpenPage<CharacterSelectUIPage>();
                break;
            case GameState.Game:
                uiManager.OpenPage<GamingUIPage>();
                break;
            case GameState.GameOver:
                uiManager.OpenPage<GameOverUIPage>();
                break;
            case GameState.StageComplete:
                uiManager.OpenPage<StageCompleteUIPage>();
                break;
            case GameState.WaveTransition:
                uiManager.OpenPage<WaveTransitionUIPage>();
                break;
            case GameState.Shop:
                uiManager.OpenPage<ShopUIPage>();
                break;
        }
    }

    private void OpenPauseMenu()
    {
        if (uiManager.IsPageOpen<GamePauseMenu>())
        {
            return;
        }

        uiManager.OpenPage<GamePauseMenu>();
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
