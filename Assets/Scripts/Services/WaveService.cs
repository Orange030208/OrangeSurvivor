using System;
using Orange.GameServices;
using UnityEngine;

/// <summary>
/// 波次运行服务：负责波次推进、计时和刷怪导演调度。
/// </summary>
[Serializable]
public sealed class WaveService : GameService, IWaveController
{
    private const int COUNTDOWN_WARNING_SECONDS = 5;

    [SerializeField] private StageDirectorProfileSO stageDirectorProfile;
    [SerializeField] private Entity spawnAroundEntity;
    [SerializeField] private Transform spawnParent;

    private WaveDirectorRuntimeSession directorSession;
    private EnemyFactory enemyFactory;
    private RunProgressionService runProgressionService;
    private WaveRuntimeState runtimeState = WaveRuntimeState.CreateIdle();
    private IWaveCompletionRule currentCompletionRule = new TimerOnlyWaveCompletionRule();
    private int lastCountdownSecond = -1;

    public override GameServiceTickMode TickMode => GameServiceTickMode.Update;

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
    private UnityEngine.Object LogContext => Context != null ? Context.Root : null;
    private Transform SpawnParent => spawnParent != null ? spawnParent : Context?.RootTransform;

    public bool HasMoreWaves => HasStarted && directorSession != null && directorSession.HasNextWave(CurrentWaveIndex);
    public bool HasCurrentWave => HasStarted;
    public WaveRuntimeState CurrentState => runtimeState;

    protected override void DeclareDependencies(GameServiceDependencyBuilder dependencies)
    {
        dependencies.Require<IGameContentProvider>();
        dependencies.Require<IEnemyRegistry>();
    }

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IWaveController>(this);
    }

    protected override void OnValidateService(GameServiceValidationReport report)
    {
        if (stageDirectorProfile != null &&
            stageDirectorProfile.FiniteWaveCount == 0 &&
            !stageDirectorProfile.SupportsEndless)
        {
            report.AddError(
                $"{nameof(WaveService)} requires at least one finite wave or a valid endless profile in {stageDirectorProfile.name}.",
                GetType());
        }
    }

    protected override void OnAttach()
    {
        if (stageDirectorProfile == null && GameContentRuntime.TryGetProvider(out IGameContentProvider provider))
        {
            stageDirectorProfile = provider.DefaultStageDirectorProfile;
        }

        if (stageDirectorProfile == null)
        {
            throw new MissingReferenceException(
                $"{nameof(WaveService)} requires a {nameof(StageDirectorProfileSO)} from the service or {nameof(GameContentCatalogSO)}.");
        }

        if (stageDirectorProfile.FiniteWaveCount == 0 && !stageDirectorProfile.SupportsEndless)
        {
            throw new MissingReferenceException(
                $"{nameof(WaveService)} requires at least one finite wave or a valid endless profile in {stageDirectorProfile.name}.");
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

        YokiFrame.EventKit.Type.Register<PlayerSpawnedEvent>(OnPlayerSpawned);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<PlayerSpawnedEvent>(OnPlayerSpawned));

        YokiFrame.EventKit.Type.Register<EnemyRegisteredEvent>(OnEnemyRegistered);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<EnemyRegisteredEvent>(OnEnemyRegistered));

        YokiFrame.EventKit.Type.Register<EnemyUnregisteredEvent>(OnEnemyUnregistered);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<EnemyUnregisteredEvent>(OnEnemyUnregistered));

        YokiFrame.EventKit.Type.Register<EntityDiedEvent>(OnEntityDied);
        AddCleanup(() => YokiFrame.EventKit.Type.UnRegister<EntityDiedEvent>(OnEntityDied));

        TryBindSpawnAnchor();
        PublishWaveRuntimeChanged();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (!IsTimerOn || runtimeState.CompletionTriggered)
        {
            return;
        }

        float previousTime = CurrentTimer;
        float nextTime = previousTime + deltaTime;
        ProcessCurrentWaveSpawns(previousTime, nextTime);
        runtimeState.Timer = nextTime;
        runProgressionService?.Tick(deltaTime);

        if (CurrentTimer >= CurrentWaveDuration)
        {
            runtimeState.Timer = CurrentWaveDuration;
            HandleWaveTimerElapsed();
            return;
        }

        TryPlayCountdownTick();
        PublishWaveProgress();
    }

    protected override void OnDispose()
    {
        RunProgressionRuntime.ClearProvider(runProgressionService);
        directorSession = null;
        enemyFactory = null;
        runProgressionService = null;
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
            Debug.LogWarning($"{nameof(WaveService)}: 没有下一波可以开始。", LogContext);
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
            SpawnParent,
            runProgressionService);
        directorSession.Advance(previousTime, currentTime, context);
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex < 0)
        {
            Debug.LogWarning($"{nameof(WaveService)}: 非法波次索引 {waveIndex}", LogContext);
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
            Debug.LogError(decision.DiagnosticError, LogContext);
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

        Player resolvedPlayer = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (resolvedPlayer != null)
        {
            spawnAroundEntity = resolvedPlayer;
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
