using UnityEngine;

using System;

public readonly struct WaveSpawnIdentity
{
    public string TrackId { get; }
    public WaveSpawnTriggerMode TriggerMode { get; }
    public WaveEnemySpawnOption[] EnemyPool { get; }
    public float SpawnFrequency { get; }
    public int SpawnCountPerBatch { get; }
    public int MaxSpawnBatches { get; }
    public Vector2 NormalizedTimeRange { get; }

    public WaveSpawnIdentity(EnemySO enemyDefinition, float spawnFrequency, int spawnCountPerBatch, Vector2 normalizedTimeRange)
        : this(
            "Track",
            WaveSpawnTriggerMode.Interval,
            enemyDefinition != null
                ? new[] { new WaveEnemySpawnOption(enemyDefinition, 1f, WaveEnemyTag.Normal) }
                : Array.Empty<WaveEnemySpawnOption>(),
            spawnFrequency,
            spawnCountPerBatch,
            0,
            normalizedTimeRange)
    {
    }

    public WaveSpawnIdentity(
        string trackId,
        WaveSpawnTriggerMode triggerMode,
        WaveEnemySpawnOption[] enemyPool,
        float spawnFrequency,
        int spawnCountPerBatch,
        int maxSpawnBatches,
        Vector2 normalizedTimeRange)
    {
        TrackId = string.IsNullOrWhiteSpace(trackId) ? "Track" : trackId;
        TriggerMode = triggerMode;
        EnemyPool = enemyPool ?? Array.Empty<WaveEnemySpawnOption>();
        SpawnFrequency = spawnFrequency;
        SpawnCountPerBatch = spawnCountPerBatch;
        MaxSpawnBatches = maxSpawnBatches;
        NormalizedTimeRange = normalizedTimeRange;
    }

    public EnemySO EnemyDefinition
    {
        get
        {
            for (int i = 0; i < EnemyPool.Length; i++)
            {
                if (EnemyPool[i].EnemyDefinition != null)
                {
                    return EnemyPool[i].EnemyDefinition;
                }
            }

            return null;
        }
    }
}
