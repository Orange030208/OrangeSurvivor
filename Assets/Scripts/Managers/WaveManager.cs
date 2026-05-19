using UnityEngine;

/// <summary>
/// 敌波管理器：负责波次数据推进、计时和刷怪。
/// 波末状态切换由 GameManager 根据玩家本波收益决定。
/// </summary>
public class WaveManager : MonoBehaviour, IWaveController
{
    private const int COUNTDOWN_WARNING_SECONDS = 5;

    [SerializeField] private StageDefinitionSO stageDefinition;
    [SerializeField] private ContentPoolSO waveSpawnPool;
    private SpawnPositionResolver spawnPositionResolver;
    private WaveSpawnExecutor waveSpawnExecutor;
    private EnemyFactory enemyFactory;
    private RunProgressionService runProgressionService;
    private WaveRuntimeState runtimeState = WaveRuntimeState.CreateIdle();
    private Wave[] runtimeWaves = System.Array.Empty<Wave>();
    private IWaveCompletionRule currentCompletionRule = new TimerOnlyWaveCompletionRule();
    private int lastCountdownSecond = -1;

    [SerializeField]
    private Entity spawnAroundEntity;

    private int CurrentWaveIndex => runtimeState.CurrentWaveIndex;
    private float CurrentTimer => runtimeState.Timer;
    private bool IsTimerOn => runtimeState.IsRunning;
    private int CurrentWave => CurrentWaveIndex >= 0 ? CurrentWaveIndex + 1 : 0;
    private int TotalWaves => runtimeWaves.Length;
    private bool HasStarted => CurrentWaveIndex >= 0;
    public bool HasMoreWaves => CurrentWaveIndex >= 0 && CurrentWaveIndex < TotalWaves - 1;
    public bool HasCurrentWave => HasStarted;
    public WaveRuntimeState CurrentState => runtimeState;
    private float CurrentWaveDuration => GetWaveDuration(CurrentWaveIndex);

    private void Awake()
    {
        if (stageDefinition == null)
        {
            stageDefinition = GameContentRuntime.Provider.DefaultStageDefinition;
        }

        if (stageDefinition == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires a {nameof(StageDefinitionSO)} from the scene or {nameof(GameContentCatalogSO)}.");
        }

        runtimeWaves = WaveDefinitionMapper.ToRuntimeWaves(stageDefinition);
        if (runtimeWaves.Length == 0)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires at least one valid wave in {stageDefinition.name}.");
        }

        if (FindFirstObjectByType<EnemyRegistry>() == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires an active {nameof(EnemyRegistry)} in the scene.");
        }

        if (waveSpawnPool == null)
        {
            waveSpawnPool = GameContentRuntime.Provider.WaveSpawnPool;
        }

        if (waveSpawnPool == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires a {nameof(ContentPoolSO)} for wave spawn candidates from the scene or {nameof(GameContentCatalogSO)}.");
        }

        enemyFactory = new EnemyFactory();
        RunProgressionProfileSO progressionProfile = GameContentRuntime.Provider.RunProgressionProfile;
        runProgressionService = new RunProgressionService(progressionProfile);
        runProgressionService.Reset(runtimeWaves.Length);
        RunProgressionRuntime.SetProvider(runProgressionService);
        waveSpawnExecutor = new WaveSpawnExecutor(enemyFactory, waveSpawnPool);
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<EnemyRegisteredEvent>(OnEnemyRegistered);
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);

        TryBindSpawnAnchor();
        PublishWaveRuntimeChanged();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<EnemyRegisteredEvent>(OnEnemyRegistered);
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        RunProgressionRuntime.ClearProvider(runProgressionService);
    }

    private void Update()
    {
        if (!IsTimerOn || runtimeState.CompletionTriggered)
        {
            return;
        }

        ProcessCurrentWaveSpawns();
        runtimeState.Timer += Time.deltaTime;
        runProgressionService?.Tick(Time.deltaTime);
        if (CurrentTimer >= CurrentWaveDuration)
        {
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
        if (runtimeState.CompletionTriggered || CurrentWaveIndex < 0 || CurrentWaveIndex >= runtimeWaves.Length)
        {
            return;
        }

        ApplyCompletionDecision(currentCompletionRule.OnEnemyRegistered(eventData.Role, CreateCompletionContext()));
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (runtimeState.CompletionTriggered || CurrentWaveIndex < 0 || CurrentWaveIndex >= runtimeWaves.Length)
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
        if (TotalWaves == 0)
        {
            Debug.LogWarning("WaveManager: 没有配置任何波次数据！");
            return;
        }

        int nextWaveIndex = CurrentWaveIndex + 1;
        if (nextWaveIndex < 0)
        {
            nextWaveIndex = 0;
        }

        if (nextWaveIndex >= TotalWaves)
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

    private void ProcessCurrentWaveSpawns()
    {
        if (CurrentWaveIndex < 0 || CurrentWaveIndex >= runtimeWaves.Length)
        {
            return;
        }

        Wave currentWave = runtimeWaves[CurrentWaveIndex];
        WaveSegment[] segments = currentWave.Segments;
        if (segments == null || runtimeState.SegmentStates == null || segments.Length != runtimeState.SegmentStates.Length)
        {
            return;
        }

        float waveDuration = CurrentWaveDuration;
        WaveSpawnContext spawnContext = new WaveSpawnContext(
            CurrentWaveIndex,
            CurrentWave,
            currentWave.WaveId,
            currentWave.Name,
            CurrentTimer,
            waveDuration,
            spawnAroundEntity,
            transform,
            runProgressionService);
        waveSpawnExecutor.ExecuteModifierOnlyRequests(spawnContext, spawnPositionResolver);

        for (int i = 0; i < segments.Length; i++)
        {
            WaveSpawnExecutionRequest request = new WaveSpawnExecutionRequest(
                segments[i],
                i,
                CurrentTimer,
                waveDuration,
                CurrentWaveIndex,
                currentWave.WaveId,
                currentWave.Name,
                spawnAroundEntity,
                transform,
                runProgressionService);
            WaveSegmentRuntimeState segmentState = runtimeState.SegmentStates[i];
            WaveSpawnExecutionResult result = waveSpawnExecutor.Execute(request, segmentState, spawnPositionResolver);
            runtimeState.SegmentStates[i] = result.SegmentState;
        }
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= runtimeWaves.Length)
        {
            Debug.LogWarning($"WaveManager: 非法波次索引 {waveIndex}");
            return;
        }

        TryBindSpawnAnchor();
        ApplySpawnPositionResolver(waveIndex);
        Wave wave = runtimeWaves[waveIndex];
        currentCompletionRule = WaveCompletionRuleFactory.Create(wave.CompletionMode);
        runtimeState = new WaveRuntimeState(
            waveIndex,
            0f,
            true,
            CreateSegmentStates(wave),
            false);
        runProgressionService?.BeginWave(CurrentWave, TotalWaves);
        ResetCountdownTickState();

        PublishWaveRuntimeChanged();
        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        GameEventBus.Publish(new WaveProgressEvent(
            CurrentWaveDuration,
            CurrentWaveDuration,
            currentCompletionRule.ShowsCountdownTimer));
        WaveSpawnModifierRegistry.NotifyWaveStarted(new WaveSpawnContext(
            CurrentWaveIndex,
            CurrentWave,
            wave.WaveId,
            wave.Name,
            0f,
            CurrentWaveDuration,
            spawnAroundEntity,
            transform,
            runProgressionService));
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
        Wave currentWave = runtimeWaves[CurrentWaveIndex];
        WaveSpawnModifierRegistry.NotifyWaveEnded(new WaveSpawnContext(
            CurrentWaveIndex,
            CurrentWave,
            currentWave.WaveId,
            currentWave.Name,
            CurrentTimer,
            CurrentWaveDuration,
            spawnAroundEntity,
            transform,
            runProgressionService));
        PublishWaveRuntimeChanged();
        WaveCompletedEvent completedEvent = new WaveCompletedEvent(
            CurrentWave,
            TotalWaves,
            CurrentTimer,
            HasMoreWaves);
        GameEventBus.Publish(completedEvent);
    }

    private WaveSegmentRuntimeState[] CreateSegmentStates(Wave wave)
    {
        WaveSegment[] segments = wave.Segments;
        if (segments == null || segments.Length == 0)
        {
            return System.Array.Empty<WaveSegmentRuntimeState>();
        }

        WaveSegmentRuntimeState[] states = new WaveSegmentRuntimeState[segments.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = WaveSegmentRuntimeState.CreateDefault();
        }

        return states;
    }

    private float GetWaveDuration(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= runtimeWaves.Length)
        {
            return 0f;
        }

        return runtimeWaves[waveIndex].Duration;
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
        if (!HasStarted || TotalWaves == 0)
        {
            return;
        }

        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        PublishWaveProgress();
    }

    private void PublishWaveProgress()
    {
        float waveDuration = CurrentWaveDuration;
        float remaining = CalculateRemainingWaveTime(waveDuration);
        GameEventBus.Publish(new WaveProgressEvent(
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
        GameEventBus.Publish(new WaveRuntimeChangedEvent(
            viewData.CurrentWave,
            viewData.TotalWaves,
            viewData.HasStarted,
            viewData.HasMoreWaves,
            viewData.IsRunning,
            viewData.ElapsedTime,
            viewData.CurrentWaveDuration));
    }

    private void ApplySpawnPositionResolver(int waveIndex)
    {
        SpawnLocationDefinition spawnLocation = GetRequiredSpawnLocation(waveIndex);
        spawnPositionResolver = SpawnPositionResolver.FromDefinition(spawnLocation);
    }

    private SpawnLocationDefinition GetRequiredSpawnLocation(int waveIndex)
    {
        if (runtimeWaves == null || waveIndex < 0 || waveIndex >= runtimeWaves.Length)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} does not contain a valid runtime wave at index {waveIndex}.");
        }

        SpawnLocationDefinition spawnLocation = runtimeWaves[waveIndex].SpawnLocation;
        if (spawnLocation == null)
        {
            throw new MissingReferenceException($"{runtimeWaves[waveIndex].WaveId} is missing {nameof(SpawnLocationDefinition)}.");
        }

        return spawnLocation;
    }
}
