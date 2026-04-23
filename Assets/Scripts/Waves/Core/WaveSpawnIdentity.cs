using UnityEngine;

public readonly struct WaveSpawnIdentity
{
    public EnemySO EnemyDefinition { get; }
    public float SpawnFrequency { get; }
    public int SpawnCountPerBatch { get; }
    public Vector2 NormalizedTimeRange { get; }

    public WaveSpawnIdentity(EnemySO enemyDefinition, float spawnFrequency, int spawnCountPerBatch, Vector2 normalizedTimeRange)
    {
        EnemyDefinition = enemyDefinition;
        SpawnFrequency = spawnFrequency;
        SpawnCountPerBatch = spawnCountPerBatch;
        NormalizedTimeRange = normalizedTimeRange;
    }
}
