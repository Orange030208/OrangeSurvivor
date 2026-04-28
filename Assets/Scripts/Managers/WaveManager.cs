using UnityEngine;

/// <summary>
/// 敌波管理器：只负责波次数据推进、计时和刷怪。
/// 状态切换与流程编排交给 WaveFlowCoordinator / GameManager。
/// </summary>
public class WaveManager : MonoBehaviour
{
    private StageDefinitionSO stageDefinition;
    private SpawnPositionResolver spawnPositionResolver;
    private WaveCompletionService waveCompletionService;
    private WaveSpawnExecutionService waveSpawnExecutionService;
    private EnemyRuntimeRegistry enemyRuntimeRegistry;
    private EnemyFactory enemyFactory;
    private WaveRuntimeState runtimeState = WaveRuntimeState.CreateIdle();
    private Wave[] runtimeWaves = System.Array.Empty<Wave>();

    [SerializeField]
    private Entity spawnAroundEntity;
    [SerializeField] private SpawnIndicator enemySpawnIndicatorPrefab;

    private int CurrentWaveIndex => runtimeState.CurrentWaveIndex;
    private float CurrentTimer => runtimeState.Timer;
    private bool IsTimerOn => runtimeState.IsRunning;
    private int CurrentWave => CurrentWaveIndex >= 0 ? CurrentWaveIndex + 1 : 0;
    private int TotalWaves => runtimeWaves.Length;
    private bool HasStarted => CurrentWaveIndex >= 0;
    private bool HasMoreWaves => CurrentWaveIndex >= 0 && CurrentWaveIndex < TotalWaves - 1;
    private float CurrentWaveDuration => GetWaveDuration(CurrentWaveIndex);

    private void Awake()
    {
        stageDefinition = ResourcesManager.GetStageDefinition();
        if (stageDefinition == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires a {nameof(StageDefinitionSO)} resource at Data/Waves/Stage Definition.");
        }

        runtimeWaves = WaveDefinitionMapper.ToRuntimeWaves(stageDefinition);
        if (runtimeWaves.Length == 0)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires at least one valid wave in {stageDefinition.name}.");
        }

        enemyRuntimeRegistry = FindFirstObjectByType<EnemyRuntimeRegistry>();
        if (enemyRuntimeRegistry == null)
        {
            throw new MissingReferenceException($"{nameof(WaveManager)} requires an active {nameof(EnemyRuntimeRegistry)} in the scene.");
        }

        enemyFactory = new EnemyFactory(enemySpawnIndicatorPrefab);
        waveCompletionService = new WaveCompletionService(enemyRuntimeRegistry);
        waveSpawnExecutionService = new WaveSpawnExecutionService(enemyFactory);
        ApplySpawnPositionPolicy(0);
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveHudSnapshotEvent>(PublishWaveHudSnapshot);
        GameEventBus.Subscribe<RequestWaveRuntimeSnapshotEvent>(PublishWaveRuntimeSnapshot);
        GameEventBus.Subscribe<StartFirstWaveRequestedEvent>(OnStartFirstWaveRequested);
        GameEventBus.Subscribe<StartNextWaveRequestedEvent>(OnStartNextWaveRequested);
        GameEventBus.Subscribe<StopCurrentWaveRequestedEvent>(OnStopCurrentWaveRequested);
        GameEventBus.Subscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
        GameEventBus.Subscribe<DefeatAllEnemiesRequestedEvent>(OnDefeatAllEnemiesRequested);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);

        TryBindSpawnAnchor();
        PublishWaveRuntimeSnapshot();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWaveHudSnapshotEvent>(PublishWaveHudSnapshot);
        GameEventBus.Unsubscribe<RequestWaveRuntimeSnapshotEvent>(PublishWaveRuntimeSnapshot);
        GameEventBus.Unsubscribe<StartFirstWaveRequestedEvent>(OnStartFirstWaveRequested);
        GameEventBus.Unsubscribe<StartNextWaveRequestedEvent>(OnStartNextWaveRequested);
        GameEventBus.Unsubscribe<StopCurrentWaveRequestedEvent>(OnStopCurrentWaveRequested);
        GameEventBus.Unsubscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
        GameEventBus.Unsubscribe<DefeatAllEnemiesRequestedEvent>(OnDefeatAllEnemiesRequested);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        GameEventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void Update()
    {
        if (!IsTimerOn || runtimeState.CompletionTriggered)
        {
            return;
        }

        ProcessCurrentWaveSpawns();
        WaveCompletionCheckResult completionResult = waveCompletionService.EvaluatePerFrame(runtimeState, CurrentWaveDuration);
        if (completionResult.ShouldComplete)
        {
            CompleteCurrentWave(completionResult.CompletionReason);
            return;
        }

        runtimeState.Timer += Time.deltaTime;
        PublishWaveProgress();
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        spawnAroundEntity = eventData.Player;
    }

    private void OnStartFirstWaveRequested()
    {
        StartWave(0);
    }

    private void OnStartNextWaveRequested()
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

    private void OnStopCurrentWaveRequested()
    {
        runtimeState.IsRunning = false;
        PublishWaveRuntimeSnapshot();
    }

    private void OnResetWavesRequested()
    {
        runtimeState = WaveRuntimeState.CreateIdle();
        PublishWaveRuntimeSnapshot();
    }

    private void OnDefeatAllEnemiesRequested()
    {
        GameEventBus.Publish<DefeatAllTrackedEnemiesRequestedEvent>();
    }

    private void OnEntityDied(EntityDiedEvent eventData)
    {
        if (!IsTimerOn || runtimeState.CompletionTriggered)
        {
            return;
        }

        WaveCompletionCheckResult completionResult = waveCompletionService.EvaluateEntityDeath(runtimeState, eventData);
        if (completionResult.ShouldComplete)
        {
            CompleteCurrentWave(completionResult.CompletionReason);
        }
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
        for (int i = 0; i < segments.Length; i++)
        {
            WaveSpawnExecutionRequest request = new WaveSpawnExecutionRequest(
                segments[i],
                i,
                CurrentTimer,
                waveDuration,
                CurrentWaveIndex,
                spawnAroundEntity,
                transform);
            WaveSegmentRuntimeState segmentState = runtimeState.SegmentStates[i];
            WaveSpawnExecutionResult result = waveSpawnExecutionService.Execute(request, segmentState, spawnPositionResolver);
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
        ApplySpawnPositionPolicy(waveIndex);
        Wave wave = runtimeWaves[waveIndex];
        runtimeState = new WaveRuntimeState(
            waveIndex,
            0f,
            true,
            CreateSegmentStates(wave),
            wave.CompletionType,
            wave.WaveTags,
            wave.RewardSnapshot,
            wave.FlowSnapshot,
            false);

        PublishWaveRuntimeSnapshot();
        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        GameEventBus.Publish(new WaveProgressEvent(CurrentWaveDuration, CurrentWaveDuration));
    }

    private void CompleteCurrentWave(WaveCompletionReason completionReason)
    {
        if (runtimeState.CompletionTriggered)
        {
            return;
        }

        runtimeState.IsRunning = false;
        runtimeState.CompletionTriggered = true;
        PublishWaveRuntimeSnapshot();
        WaveCompletedEvent completedEvent = new WaveCompletedEvent(
            CurrentWave,
            TotalWaves,
            completionReason,
            CurrentTimer,
            HasMoreWaves);
        GameEventBus.Publish(completedEvent);
        GameEventBus.Publish(new WaveRewardGrantedEvent(
            CurrentWave,
            completedEvent,
            runtimeState.RewardSnapshot,
            runtimeState.FlowSnapshot));
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

    private void PublishWaveHudSnapshot()
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
        float remaining = IsTimerOn ? Mathf.Max(0, waveDuration - CurrentTimer) : waveDuration;
        GameEventBus.Publish(new WaveProgressEvent(remaining, waveDuration));
    }

    private void PublishWaveRuntimeSnapshot()
    {
        GameEventBus.Publish(new WaveRuntimeChangedEvent(
            CurrentWave,
            TotalWaves,
            HasStarted,
            HasMoreWaves,
            IsTimerOn,
            CurrentTimer,
            HasStarted ? CurrentWaveDuration : 0f,
            runtimeState.CompletionType,
            runtimeState.WaveTags));
    }

    private void ApplySpawnPositionPolicy(int waveIndex)
    {
        SpawnLocationPolicySO policy = GetRequiredSpawnLocationPolicy(waveIndex);
        spawnPositionResolver = SpawnPositionResolver.FromPolicy(policy);
    }

    private SpawnLocationPolicySO GetRequiredSpawnLocationPolicy(int waveIndex)
    {
        WaveDefinitionSO[] definitionWaves = stageDefinition.Waves;
        if (definitionWaves == null || waveIndex < 0 || waveIndex >= definitionWaves.Length)
        {
            throw new MissingReferenceException($"{nameof(StageDefinitionSO)} does not contain a valid wave at index {waveIndex}.");
        }

        WaveDefinitionSO waveDefinition = definitionWaves[waveIndex];
        if (waveDefinition == null)
        {
            throw new MissingReferenceException($"Wave definition at index {waveIndex} is missing.");
        }

        if (waveDefinition.SpawnLocationPolicy == null)
        {
            throw new MissingReferenceException($"{waveDefinition.name} is missing {nameof(SpawnLocationPolicySO)}.");
        }

        return waveDefinition.SpawnLocationPolicy;
    }
}
