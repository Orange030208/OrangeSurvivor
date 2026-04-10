using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏主状态管理器：负责全局游戏状态切换、暂停控制、回菜单以及部分流程事件响应。
/// 它不直接操作页面细节，只通过事件与 UI 层协作。
/// </summary>
public class GameManager : MonoSingletonBase<GameManager>
{
    [SerializeField] private Player player;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameState initialGameState = GameState.Menu;

    private GameState currentGameState = GameState.None;
    private bool isPaused;

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Subscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Subscribe<WeaponSelectionCompletedEvent>(OnWeaponSelectionCompleted);
        GameEventBus.Subscribe<MenuStartClickedEvent>(OnMenuStartClicked);
        GameEventBus.Subscribe<ShopContinueClickedEvent>(OnShopContinueClicked);
        GameEventBus.Subscribe<GameOverRestartClickedEvent>(OnGameOverRestartClicked);
        GameEventBus.Subscribe<GameOverReturnToMenuClickedEvent>(OnGameOverReturnToMenuClicked);
        GameEventBus.Subscribe<GameStateChangeRequestEvent>(OnGameStateChangeRequested);
        GameEventBus.Subscribe<PauseGameRequestedEvent>(OnPauseGameRequested);
        GameEventBus.Subscribe<ResumeGameRequestedEvent>(OnResumeGameRequested);
        GameEventBus.Subscribe<ReturnToMenuRequestedEvent>(OnReturnToMenuRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Unsubscribe<UpgradeSelectionCompletedEvent>(OnUpgradeSelectionCompleted);
        GameEventBus.Unsubscribe<WeaponSelectionCompletedEvent>(OnWeaponSelectionCompleted);
        GameEventBus.Unsubscribe<MenuStartClickedEvent>(OnMenuStartClicked);
        GameEventBus.Unsubscribe<ShopContinueClickedEvent>(OnShopContinueClicked);
        GameEventBus.Unsubscribe<GameOverRestartClickedEvent>(OnGameOverRestartClicked);
        GameEventBus.Unsubscribe<GameOverReturnToMenuClickedEvent>(OnGameOverReturnToMenuClicked);
        GameEventBus.Unsubscribe<GameStateChangeRequestEvent>(OnGameStateChangeRequested);
        GameEventBus.Unsubscribe<PauseGameRequestedEvent>(OnPauseGameRequested);
        GameEventBus.Unsubscribe<ResumeGameRequestedEvent>(OnResumeGameRequested);
        GameEventBus.Unsubscribe<ReturnToMenuRequestedEvent>(OnReturnToMenuRequested);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        ChangeGameState(initialGameState);
        SetPaused(false);
    }

    private void OnWaveCompleted(WaveCompletedEvent _)
    {
        ChangeGameState(GetNextStateAfterWaveCompleted());
    }

    private void OnUpgradeSelectionCompleted()
    {
        if (currentGameState != GameState.WaveTransition)
        {
            return;
        }

        ChangeGameState(GameState.Shop);
    }

    private void OnWeaponSelectionCompleted()
    {
        if (currentGameState != GameState.WeaponSelection)
        {
            return;
        }

        ChangeGameState(GameState.Game);
    }

    private void OnMenuStartClicked()
    {
        if (currentGameState != GameState.Menu)
        {
            return;
        }

        ChangeGameState(GameState.WeaponSelection);
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

        ChangeGameState(GameState.Game);
    }

    private void OnGameOverRestartClicked()
    {
        RestartGame();
    }

    private void OnGameOverReturnToMenuClicked()
    {
        SetPaused(false);
        ChangeGameState(GameState.Menu);
        SceneManager.LoadScene(0);
    }

    private void OnGameStateChangeRequested(GameStateChangeRequestEvent eventData)
    {
        if (ShouldBlockGameplayRequest(eventData.TargetState))
        {
            return;
        }

        ChangeGameState(eventData.TargetState);
    }

    private void OnPauseGameRequested()
    {
        if (currentGameState != GameState.Game || isPaused)
        {
            return;
        }

        SetPaused(true);
    }

    private void OnResumeGameRequested()
    {
        if (!isPaused)
        {
            return;
        }

        SetPaused(false);
    }

    private void OnReturnToMenuRequested()
    {
        SetPaused(false);
        ChangeGameState(GameState.Menu);
        SceneManager.LoadScene(0);
    }

    private void ChangeGameState(GameState targetState)
    {
        if (targetState == currentGameState)
        {
            return;
        }

        if (targetState != GameState.Game && isPaused)
        {
            SetPaused(false);
        }

        GameState oldState = currentGameState;
        ExitState(oldState, targetState);
        currentGameState = targetState;
        EnterState(oldState, currentGameState);
        GameEventBus.Publish(new GameStateChangedEvent(oldState, currentGameState));
    }

    private GameState GetNextStateAfterWaveCompleted()
    {
        if (waveManager != null && !waveManager.HasMoreWaves)
        {
            GameEventBus.Publish<AllWavesCompletedEvent>();
        }

        if (player != null && player.IsLevelUpInCurrentWave)
        {
            return GameState.WaveTransition;
        }

        return GameState.Shop;
    }

    private bool ShouldBlockGameplayRequest(GameState targetState)
    {
        return targetState == GameState.Game
               && (currentGameState == GameState.Shop || currentGameState == GameState.WaveTransition)
               && waveManager != null
               && !waveManager.HasMoreWaves;
    }

    private void ExitState(GameState oldState, GameState newState)
    {
        if (waveManager == null)
        {
            return;
        }

        if (oldState == GameState.Game && newState != GameState.Game)
        {
            waveManager.StopCurrentWave();
        }

        if (newState == GameState.Shop || newState == GameState.WaveTransition)
        {
            waveManager.DefeatAllEnemies();
        }

        if (newState == GameState.Menu || newState == GameState.GameOver || newState == GameState.StageComplete)
        {
            waveManager.ResetWaves();
            waveManager.DefeatAllEnemies();
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
            ManageGameOver();
        }
    }

    private void EnterGameState(GameState oldState)
    {
        if (waveManager == null)
        {
            return;
        }

        if (oldState == GameState.Shop || oldState == GameState.WaveTransition)
        {
            waveManager.StartNextWave();
            return;
        }

        waveManager.StartFirstWave();
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;
        Time.timeScale = isPaused ? 0f : 1f;
        GameEventBus.Publish(new PauseStateChangedEvent(isPaused));
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
}

public enum GameState
{
    None,
    Menu,
    WeaponSelection,
    Game,
    GameOver,
    StageComplete,
    WaveTransition,
    Shop
}
