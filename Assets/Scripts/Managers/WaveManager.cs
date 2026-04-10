using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 敌波管理器：只负责波次数据推进、计时和刷怪。
/// 状态切换与流程编排交给 GameManager。
/// </summary>
public class WaveManager : MonoSingletonBase<WaveManager>
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

    public int CurrentWave => currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0;
    public int TotalWaves => waves?.Length ?? 0;
    public bool HasStarted => currentWaveIndex >= 0;
    public bool HasMoreWaves => currentWaveIndex >= 0 && currentWaveIndex < TotalWaves - 1;

    private void OnEnable()
    {
        GameEventBus.Subscribe<RequestWaveHudSnapshotEvent>(PublishWaveHudSnapshot);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<RequestWaveHudSnapshotEvent>(PublishWaveHudSnapshot);
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

    public void StopCurrentWave()
    {
        isTimerOn = false;
    }

    public void ResetWaves()
    {
        isTimerOn = false;
        timer = 0f;
        currentWaveIndex = -1;
        counterList.Clear();
    }

    public void DefeatAllEnemies()
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
        GameEventBus.Publish(new WaveStartedEvent(CurrentWave, TotalWaves));
        GameEventBus.Publish(new WaveProgressEvent(waveDuration, waveDuration));
    }

    private void CompleteCurrentWave()
    {
        isTimerOn = false;
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
