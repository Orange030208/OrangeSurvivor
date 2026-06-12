using UnityEngine;

/// <summary>
/// 敌波管理器：负责波次推进、计时和刷怪导演调度。
/// 波末状态切换由 GameManager 根据玩家本波收益决定。
/// </summary>
public class WaveManager : MonoBehaviour, IWaveController
{
    private const int COUNTDOWN_WARNING_SECONDS = 5;

    [SerializeField] private StageDirectorProfileSO stageDirectorProfile;
    [SerializeField] private Entity spawnAroundEntity;

    private WaveDirectorRuntimeSession directorSession;
    private EnemyFactory enemyFactory;
    private RunProgressionService runProgressionService;
    private WaveRuntimeState runtimeState = WaveRuntimeState.CreateIdle();
    private IWaveCompletionRule currentCompletionRule = new TimerOnlyWaveCompletionRule();
    private int lastCountdownSecond = -1;

    private int CurrentWaveIndex => runtimeState.CurrentWaveIndex;
    private float CurrentTimer => runtimeState.Timer;
    private bool IsTimerOn => runtimeState.IsRunning;
    private bool HasStarted => CurrentWaveIndex >= 0;
    private int CurrentWave => HasStarted ? CurrentWaveIndex + 1 : 0;
    private float CurrentWaveDuration => directorSession?.CurrentDirective != null
        ? directorSession.CurrentDirective.Duration
        : 0f;
    private int TotalWaves => HasStarted && directorSession != null
        ? directorSession.GetDisplayTotalWaves(CurrentWaveIndex)
        : 0;

    public bool HasMoreWaves => HasStarted && directorSession != null && directorSession.HasNextWave(CurrentWaveIndex);
    public bool HasCurrentWave => HasStarted;
    public WaveRuntimeState CurrentState => runtimeState;

    private void Awake()
    {
        if (stageDirectorProfile == null && GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            stageDirectorProfile = provider.DefaultStageDirectorProfile;
        }

        if (stageDirectorProfile == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires a {nameof(StageDirectorProfileSO)} from the scene or {nameof(GameContentCatalogSO)}.");
        }

        if (stageDirectorProfile.FiniteWaveCount == 0 && !stageDirectorProfile.SupportsEndless)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires at least one finite wave or a valid endless profile in {stageDirectorProfile.name}.");
        }

        if (FindFirstObjectByType<EnemyRegistry>() == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires an active {nameof(EnemyRegistry)} in the scene.");
        }

        enemyFactory = new EnemyFactory();
        RunProgressionProfileSO progressionProfile = GameContentRuntime.Provider.RunProgressionProfile;
        runProgressionService = new RunProgressionService(progressionProfile);
        directorSession = new WaveDirectorRuntimeSession(
            stageDirectorProfile,
            new WaveDirectiveResolver(),
            new UnityEnemySpawnExecutor(enemyFactory));
        runProgressionService.Reset(directorSession.GetProgressionTotalWaves());
        RunProgressionRuntime.SetProvider(runProgressionService);
    }

    private void OnEnable()
    {
        YokiFrame.EventKit.Type.Register<PlayerSpawnedEvent>(OnPlayerSpawned);
        YokiFrame.EventKit.Type.Register<EnemyRegisteredEvent>(OnEnemyRegistered);
        YokiFrame.EventKit.Type.Register<EnemyUnregisteredEvent>(OnEnemyUnregistered);
        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);

        TryBindSpawnAnchor();
        PublishWaveRuntimeChanged();
    }

    private void OnDisable()
    {
        YokiFrame.EventKit.Type.UnRegister<PlayerSpawnedEvent>(OnPlayerSpawned);
        YokiFrame.EventKit.Type.UnRegister<EnemyRegisteredEvent>(OnEnemyRegistered);
        YokiFrame.EventKit.Type.UnRegister<EnemyUnregisteredEvent>(OnEnemyUnregistered);
        YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied);
        RunProgressionRuntime.ClearProvider(runProgressionService);
    }

    private void Update()
    {
        if (!IsTimerOn || runtimeState.CompletionTriggered)
        {
            return;
        }

        float previousTime = CurrentTimer;
        float nextTime = previousTime + Time.deltaTime;
        ProcessCurrentWaveSpawns(previousTime, nextTime);
        runtimeState.Timer = nextTime;
        runProgressionService?.Tick(Time.deltaTime);

        if (CurrentTimer >= CurrentWaveDuration)
        {
            runtimeState.Timer = CurrentWaveDuration;
            HandleWaveTimerElapsed();
            return;
        }

        TryPlayCountdownTick();
        PublishWaveProgress();
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        spawnAroundEntity = eventData.Player;
    }

    private void OnEnemyRegistered(EnemyRegisteredEvent eventData)
    {
        if (runtimeState.CompletionTriggered || CurrentWaveIndex < 0)
        {
            return;
        }

        ApplyCompletionDecision(currentCompletionRule.OnEnemyRegistered(eventData.Role, CreateCompletionContext()));
    }

    private void OnEnemyUnregistered(EnemyUnregisteredEvent eventData)
    {
        directorSession?.NotifyEnemyUnregistered(eventData.Enemy);
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (runtimeState.CompletionTriggered || CurrentWaveIndex < 0)
        {
            return;
        }

        if (eventData.Entity is not Enemy enemy)
        {
            return;
        }

        ApplyCompletionDecision(currentCompletionRule.OnEnemyDied(enemy.Role, CreateCompletionContext()));
    }

    public void StartFirstWave()
    {
        StartWave(0);
    }

    public void StartNextWave()
    {
        if (directorSession == null)
        {
            return;
        }

        int nextWaveIndex = CurrentWaveIndex + 1;
        if (nextWaveIndex < 0)
        {
            nextWaveIndex = 0;
        }

        if (HasStarted && !directorSession.HasNextWave(CurrentWaveIndex))
        {
            Debug.LogWarning("WaveManager: 没有下一波可以开始。");
            return;
        }

        StartWave(nextWaveIndex);
    }

    public void StopCurrentWave()
    {
        runtimeState.IsRunning = false;
        ResetCountdownTickState();
        PublishWaveRuntimeChanged();
    }

    public void ResumeCurrentWave()
    {
        if (!HasStarted || runtimeState.CompletionTriggered)
        {
            return;
        }

        runtimeState.IsRunning = true;
        PublishWaveRuntimeChanged();
    }

    public void ResetWaves()
    {
        runtimeState = WaveRuntimeState.CreateIdle();
        currentCompletionRule = new TimerOnlyWaveCompletionRule();
        ResetCountdownTickState();
        PublishWaveRuntimeChanged();
    }

    private void ProcessCurrentWaveSpawns(float previousTime, float currentTime)
    {
        if (!HasStarted || directorSession?.CurrentDirective == null)
        {
            return;
        }

        WaveDirectorExecutionContext context = new(
            CurrentWaveIndex,
            CurrentWave,
            directorSession.CurrentDirective.WaveId,
            directorSession.CurrentDirective.DisplayName,
            currentTime,
            CurrentWaveDuration,
            spawnAroundEntity,
            transform,
            runProgressionService);
        directorSession.Advance(previousTime, currentTime, context);
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex < 0)
        {
            Debug.LogWarning($"WaveManager: 非法波次索引 {waveIndex}");
            return;
        }

        TryBindSpawnAnchor();
        directorSession.BeginWave(waveIndex);
        currentCompletionRule = WaveCompletionRuleFactory.Create(directorSession.CurrentDirective.CompletionMode);
        runtimeState = new WaveRuntimeState(
            waveIndex,
            0f,
            true,
            false);
        runProgressionService?.BeginWave(CurrentWave, directorSession.GetProgressionTotalWaves());
        ResetCountdownTickState();

        PublishWaveRuntimeChanged();
        YokiFrame.EventKit.Type.Send(new WaveStartedEvent(CurrentWave, TotalWaves));
        YokiFrame.EventKit.Type.Send(new WaveProgressEvent(
            CurrentWaveDuration,
            CurrentWaveDuration,
            currentCompletionRule.ShowsCountdownTimer));
    }

    private void HandleWaveTimerElapsed()
    {
        ApplyCompletionDecision(currentCompletionRule.OnTimerElapsed(CreateCompletionContext()));
    }

    private void ApplyCompletionDecision(WaveCompletionDecision decision)
    {
        if (decision.HasDiagnosticError)
        {
            Debug.LogError(decision.DiagnosticError, this);
        }

        if (decision.StopTimer)
        {
            runtimeState.Timer = CurrentWaveDuration;
            runtimeState.IsRunning = false;
            ResetCountdownTickState();
            if (!decision.CompleteWave)
            {
                PublishWaveProgress();
                PublishWaveRuntimeChanged();
            }
        }

        if (decision.CompleteWave)
        {
            CompleteCurrentWave();
        }
    }

    private void CompleteCurrentWave()
    {
        if (runtimeState.CompletionTriggered)
        {
            return;
        }

        runtimeState.IsRunning = false;
        runtimeState.CompletionTriggered = true;
        runProgressionService?.CompleteWave(CurrentWave);
        PublishWaveRuntimeChanged();
        YokiFrame.EventKit.Type.Send(new WaveCompletedEvent(
            CurrentWave,
            TotalWaves,
            CurrentTimer,
            HasMoreWaves));
    }

    private void TryBindSpawnAnchor()
    {
        if (spawnAroundEntity != null)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            spawnAroundEntity = player;
        }
    }

    public WaveHudViewData CreateHudViewData()
    {
        float waveDuration = CurrentWaveDuration;
        float remaining = CalculateRemainingWaveTime(waveDuration);
        return new WaveHudViewData(
            CurrentWave,
            TotalWaves,
            HasStarted,
            remaining,
            waveDuration,
            HasStarted && currentCompletionRule.ShowsCountdownTimer);
    }

    public WaveRuntimeViewData CreateRuntimeViewData()
    {
        return new WaveRuntimeViewData(
            CurrentWave,
            TotalWaves,
            HasStarted,
            HasMoreWaves,
            IsTimerOn,
            CurrentTimer,
            HasStarted ? CurrentWaveDuration : 0f);
    }

    public void PublishCurrentHud()
    {
        if (!HasStarted)
        {
            return;
        }

        YokiFrame.EventKit.Type.Send(new WaveStartedEvent(CurrentWave, TotalWaves));
        PublishWaveProgress();
    }

    private void PublishWaveProgress()
    {
        float waveDuration = CurrentWaveDuration;
        float remaining = CalculateRemainingWaveTime(waveDuration);
        YokiFrame.EventKit.Type.Send(new WaveProgressEvent(
            remaining,
            waveDuration,
            HasStarted && currentCompletionRule.ShowsCountdownTimer));
    }

    private float CalculateRemainingWaveTime(float waveDuration)
    {
        if (!HasStarted)
        {
            return waveDuration;
        }

        return Mathf.Max(0f, waveDuration - CurrentTimer);
    }

    private void TryPlayCountdownTick()
    {
        if (!IsTimerOn || !HasStarted || !currentCompletionRule.PlaysCountdownWarning)
        {
            return;
        }

        int remainingSeconds = Mathf.CeilToInt(Mathf.Max(0f, CurrentWaveDuration - CurrentTimer));
        if (remainingSeconds <= 0 || remainingSeconds > COUNTDOWN_WARNING_SECONDS)
        {
            return;
        }

        if (remainingSeconds == lastCountdownSecond)
        {
            return;
        }

        lastCountdownSecond = remainingSeconds;
        AudioSfxBridge.RequestPlay(AudioSfxKey.WaveCountdownTick);
    }

    private void ResetCountdownTickState()
    {
        lastCountdownSecond = -1;
    }

    private WaveCompletionContext CreateCompletionContext()
    {
        return new WaveCompletionContext(
            CurrentWaveIndex,
            CurrentWave,
            CurrentTimer,
            CurrentWaveDuration);
    }

    private void PublishWaveRuntimeChanged()
    {
        WaveRuntimeViewData viewData = CreateRuntimeViewData();
        YokiFrame.EventKit.Type.Send(new WaveRuntimeChangedEvent(
            viewData.CurrentWave,
            viewData.TotalWaves,
            viewData.HasStarted,
            viewData.HasMoreWaves,
            viewData.IsRunning,
            viewData.ElapsedTime,
            viewData.CurrentWaveDuration));
    }
}
