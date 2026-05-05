using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条刷怪计划。
/// 描述一条刷怪轨道在一段归一化时间区间内，以什么节奏从敌人池中生成敌人。
/// </summary>
[Serializable]
public struct WaveSpawnPlan
{
    private const int MIN_SPAWN_COUNT_PER_BATCH = 1;
    private const float MIN_SPAWN_FREQUENCY = 0.01f;

    [SerializeField] private string trackId;
    [SerializeField] private WaveSpawnTriggerMode triggerMode;
    [SerializeField] private EnemySO enemyDefinition;
    [SerializeField] private WaveEnemyTag enemyTags;
    [SerializeField] private WaveEnemySpawnOption[] enemyPool;
    [SerializeField] private float spawnFrequency;
    [SerializeField] private int spawnCountPerBatch;
    [SerializeField] private int maxSpawnBatches;
    [SerializeField] private Vector2 normalizedTimeRange;

    public string TrackId => string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
    public WaveSpawnTriggerMode TriggerMode => triggerMode;
    public EnemySO EnemyDefinition => enemyDefinition;
    public WaveEnemyTag EnemyTags => enemyTags == WaveEnemyTag.None ? WaveEnemyTag.Normal : enemyTags;
    public WaveEnemySpawnOption[] EnemyPool => enemyPool;
    public float SpawnFrequency => Mathf.Max(MIN_SPAWN_FREQUENCY, spawnFrequency);
    public int SpawnCountPerBatch => Mathf.Max(MIN_SPAWN_COUNT_PER_BATCH, spawnCountPerBatch);
    public int MaxSpawnBatches => Mathf.Max(0, maxSpawnBatches);
    public Vector2 NormalizedTimeRange => normalizedTimeRange;

    public WaveSpawnPlan(EnemySO enemyDefinition, float spawnFrequency, int spawnCountPerBatch, Vector2 normalizedTimeRange)
    {
        trackId = "Track";
        triggerMode = WaveSpawnTriggerMode.Interval;
        this.enemyDefinition = enemyDefinition;
        enemyTags = WaveEnemyTag.Normal;
        enemyPool = Array.Empty<WaveEnemySpawnOption>();
        this.spawnFrequency = spawnFrequency;
        this.spawnCountPerBatch = spawnCountPerBatch;
        maxSpawnBatches = 0;
        this.normalizedTimeRange = normalizedTimeRange;
    }

    public WaveSpawnPlan(
        string trackId,
        WaveSpawnTriggerMode triggerMode,
        IReadOnlyList<WaveEnemySpawnOption> enemyPool,
        float spawnFrequency,
        int spawnCountPerBatch,
        int maxSpawnBatches,
        Vector2 normalizedTimeRange)
    {
        this.trackId = string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
        this.triggerMode = triggerMode;
        enemyDefinition = null;
        enemyTags = WaveEnemyTag.Normal;
        this.enemyPool = ToArray(enemyPool);
        this.spawnFrequency = spawnFrequency;
        this.spawnCountPerBatch = spawnCountPerBatch;
        this.maxSpawnBatches = maxSpawnBatches;
        this.normalizedTimeRange = normalizedTimeRange;
    }

    public WaveEnemySpawnOption[] GetEffectiveEnemyPool()
    {
        if (enemyPool != null && enemyPool.Length > 0)
        {
            return enemyPool;
        }

        return enemyDefinition != null
            ? new[] { new WaveEnemySpawnOption(enemyDefinition, 1f, EnemyTags) }
            : Array.Empty<WaveEnemySpawnOption>();
    }

    private static WaveEnemySpawnOption[] ToArray(IReadOnlyList<WaveEnemySpawnOption> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<WaveEnemySpawnOption>();
        }

        WaveEnemySpawnOption[] result = new WaveEnemySpawnOption[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            result[i] = source[i];
        }

        return result;
    }
}
