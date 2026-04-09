using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingletonBase<GameManager>
{
    [SerializeField] private Player player;
    [SerializeField] private GameState initialGameState = GameState.Menu;

    private GameState currentGameState = GameState.None;
    private bool isPaused;

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        GameEventBus.Subscribe<GameStateChangeRequestEvent>(OnGameStateChangeRequested);
        GameEventBus.Subscribe<PauseGameRequestedEvent>(OnPauseGameRequested);
        GameEventBus.Subscribe<ResumeGameRequestedEvent>(OnResumeGameRequested);
        GameEventBus.Subscribe<ReturnToMenuRequestedEvent>(OnReturnToMenuRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
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

    // private void Update()
    // {
    //     if (currentGameState != GameState.Game)
    //     {
    //         return;
    //     }
    //
    //     if (Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         if (isPaused)
    //         {
    //             GameEventBus.Publish<ResumeGameRequestedEvent>();
    //         }
    //         else
    //         {
    //             GameEventBus.Publish<PauseGameRequestedEvent>();
    //         }
    //     }
    // }

    private void OnWaveCompleted(WaveCompletedEvent _)
    {
        if (player != null && player.IsLevelUpInCurrentWave)
        {
            GameEventBus.Publish(new GameStateChangeRequestEvent(GameState.WaveTransition));
            return;
        }

        GameEventBus.Publish(new GameStateChangeRequestEvent(GameState.Shop));
    }

    private void OnGameStateChangeRequested(GameStateChangeRequestEvent eventData)
    {
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
        currentGameState = targetState;
        GameEventBus.Publish(new GameStateChangedEvent(oldState, currentGameState));

        if (currentGameState == GameState.GameOver)
        {
            ManageGameOver();
        }
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
        DOVirtual.DelayedCall(2f, () =>
        {
            SceneManager.LoadScene(0);
        }).SetUpdate(true);
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
