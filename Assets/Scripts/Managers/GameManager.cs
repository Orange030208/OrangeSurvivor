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
public class GameManager : MonoSingletonBase<GameManager>
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Player player;
    [SerializeField] private CharacterSelectionManager characterSelectionManager;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemyRegistry enemyRegistry;
    [SerializeField] private InventoryOperateManager inventoryOperateManager;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private StageCompleteSummaryManager stageCompleteSummaryManager;
    [SerializeField] private GameState initialGameState = GameState.Menu;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

    private GameState currentGameState = GameState.None;
    private bool isPaused;
    private bool hasMoreWaves;
    private readonly HashSet<string> pauseSources = new();
    private int stateTransitionVersion;
    private bool isSceneReloading;

    public GameState CurrentGameState => currentGameState;

    private void OnEnable()
    {
        ResolveSceneReferences();

        GameEventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
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
        hasMoreWaves = waveManager != null && waveManager.HasMoreWaves;
    }

    private void OnDisable()
    {
        stateTransitionVersion++;
        GameEventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
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

    private void OnWaveCompleted(WaveCompletedEvent eventData)
    {
        if (currentGameState != GameState.Game)
        {
            return;
        }

        StartWaveEndFlow(eventData);
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
        ApplyStateMusic(currentGameState);
        GameEventBus.Publish(new GameStateChangedEvent(oldState, currentGameState));
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
               && currentGameState == GameState.Shop
               && !hasMoreWaves;
    }

    private void ExitState(GameState oldState, GameState newState)
    {
        if (oldState == GameState.Game && newState != GameState.Game)
        {
            StopCurrentWave();
        }

        if (newState == GameState.Shop)
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
            case GameState.CharacterSelection:
                AudioPlaybackBridge.RequestPlayMusic(AudioBgmKey.CharacterSelection);
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
        EnsureMapGenerated();
        EnsurePlayerSpawned();

        if (oldState == GameState.Shop)
        {
            StartNextWave();
            return;
        }

        StartFirstWave();
    }

    private void StartWaveEndFlow(WaveCompletedEvent completedEvent)
    {
        DefeatAllTrackedEnemies();

        if (!completedEvent.HasNextWave)
        {
            GameEventBus.Publish<AllWavesCompletedEvent>();
            TransitionToState(GameState.StageComplete);
            return;
        }

        TransitionToState(GameState.Shop);
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
        return uiManager.ClosePageAsync<TPage>(cancellationToken);
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

        if (uiManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(UIManager)} reference.");
        }

        if (characterSelectionManager == null)
        {
            throw new MissingReferenceException($"{nameof(GameManager)} requires an explicit {nameof(CharacterSelectionManager)} reference.");
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
    CharacterSelection,
    Game,
    GameOver,
    StageComplete,
    Shop
}
