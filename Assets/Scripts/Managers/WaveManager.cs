using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 敌波管理器：只负责波次数据推进、计时和刷怪。
/// 状态切换与流程编排交给 GameManager。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [SerializeField] private float waveDuration;

    [OnValueChanged("AutoSetWaveNames")] [SerializeField]
    private Wave[] waves;

#if UNITY_EDITOR
    private void AutoSetWaveNames()
    {
        if (waves == null) return;
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].name = $"Wave {i + 1}";
        }
    }
#endif

    private readonly List<int> counterList = new();
    private int currentWaveIndex = -1;
    private float timer;
    private bool isTimerOn;

    [SerializeField] private Entity spawnAroundEntity;

    private int CurrentWave => currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0;
    private int TotalWaves => waves?.Length ?? 0;
    private bool HasStarted => currentWaveIndex >= 0;
    private bool HasMoreWaves => currentWaveIndex >= 0 && currentWaveIndex < TotalWaves - 1;

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveHudSnapshotEvent>(PublishWaveHudSnapshot);
        GameEventBus.Subscribe<RequestWaveRuntimeSnapshotEvent>(PublishWaveRuntimeSnapshot);
        GameEventBus.Subscribe<StartFirstWaveRequestedEvent>(OnStartFirstWaveRequested);
        GameEventBus.Subscribe<StartNextWaveRequestedEvent>(OnStartNextWaveRequested);
        GameEventBus.Subscribe<StopCurrentWaveRequestedEvent>(OnStopCurrentWaveRequested);
        GameEventBus.Subscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
        GameEventBus.Subscribe<DefeatAllEnemiesRequestedEvent>(OnDefeatAllEnemiesRequested);
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
    }

    private void Update()
    {
        if (!isTimerOn)
        {
            return;
        }

        if (timer < waveDuration)
        {
            ProcessCurrentWaveSpawns();
            timer += Time.deltaTime;

            float remaining = Mathf.Max(0, waveDuration - timer);
            GameEventBus.Publish(new WaveProgressEvent(remaining, waveDuration));
            return;
        }

        CompleteCurrentWave();
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

        int nextWaveIndex = currentWaveIndex + 1;
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
        isTimerOn = false;
        PublishWaveRuntimeSnapshot();
    }

    private void OnResetWavesRequested()
    {
        isTimerOn = false;
        timer = 0f;
        currentWaveIndex = -1;
        counterList.Clear();
        PublishWaveRuntimeSnapshot();
    }

    private void OnDefeatAllEnemiesRequested()
    {
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.PassAwayAfterWave();
        }
    }

    private void ProcessCurrentWaveSpawns()
    {
        if (waves == null || currentWaveIndex < 0 || currentWaveIndex >= waves.Length)
        {
            return;
        }

        Wave currentWave = waves[currentWaveIndex];

        for (int i = 0; i < currentWave.segments.Count; i++)
        {
            WaveSegment segment = currentWave.segments[i];
            float tStart = segment.timeStartEnd.x / 100f * waveDuration;
            float tEnd = segment.timeStartEnd.y / 100f * waveDuration;

            if (timer < tStart || timer > tEnd)
            {
                continue;
            }

            float timeSinceSegmentStart = timer - tStart;
            float spawnDelay = 1f / segment.spawnFrequency;

            if (timeSinceSegmentStart / spawnDelay >= counterList[i])
            {
                Instantiate(segment.enemy.gameObject, GetSpawnPosition(spawnAroundEntity), Quaternion.identity,
                    transform);
                counterList[i]++;
            }
        }
    }

    private void StartWave(int waveIndex)
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveManager: 没有配置任何波次数据！");
            return;
        }

        if (waveIndex < 0 || waveIndex >= waves.Length)
        {
            Debug.LogWarning($"WaveManager: 非法波次索引 {waveIndex}");
            return;
        }

        currentWaveIndex = waveIndex;
        timer = 0f;
        counterList.Clear();

        for (int i = 0; i < waves[waveIndex].segments.Count; i++)
        {
            counterList.Add(0);
        }

        isTimerOn = true;
        PublishWaveRuntimeSnapshot();
        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        GameEventBus.Publish(new WaveProgressEvent(waveDuration, waveDuration));
    }

    private void CompleteCurrentWave()
    {
        isTimerOn = false;
        PublishWaveRuntimeSnapshot();
        GameEventBus.Publish(new WaveCompletedEvent(CurrentWave));
    }

    private Vector3 GetSpawnPosition(IEntity entity)
    {
        if (entity == null) return Vector3.zero;

        Vector2 direction = Random.onUnitSphere;
        Vector2 offset = direction.normalized * Random.Range(6f, 10f);
        Vector2 targetPos = (Vector2)entity.Center + offset;
        targetPos.x = Mathf.Clamp(targetPos.x, -10f, 10f);
        targetPos.y = Mathf.Clamp(targetPos.y, -10f, 10f);

        return targetPos;
    }

    private void PublishWaveHudSnapshot()
    {
        if (!HasStarted || TotalWaves == 0)
        {
            return;
        }

        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        float remaining = isTimerOn ? Mathf.Max(0, waveDuration - timer) : waveDuration;
        GameEventBus.Publish(new WaveProgressEvent(remaining, waveDuration));
    }

    private void PublishWaveRuntimeSnapshot()
    {
        GameEventBus.Publish(new WaveRuntimeChangedEvent(CurrentWave, TotalWaves, HasStarted, HasMoreWaves, isTimerOn));
    }
}

/// <summary>
/// 波次数据结构：存储一个完整波次的配置
/// </summary>
[Serializable]
public struct Wave
{
    public string name;
    public List<WaveSegment> segments;
}

/// <summary>
/// 波次分段数据：单个分段的敌人生成配置
/// </summary>
[Serializable]
public struct WaveSegment
{
    public Enemy enemy;
    public float spawnFrequency;

    [MinMaxSlider(0, 100)] public Vector2 timeStartEnd;
}
