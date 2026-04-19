using UnityEngine;

public readonly struct WaveSpawnIdentity
{
    public EnemyDefinitionSO EnemyDefinition { get; }
    public float SpawnFrequency { get; }
    public int SpawnCountPerBatch { get; }
    public Vector2 NormalizedTimeRange { get; }

    public WaveSpawnIdentity(EnemyDefinitionSO enemyDefinition, float spawnFrequency, int spawnCountPerBatch, Vector2 normalizedTimeRange)
    {
        EnemyDefinition = enemyDefinition;
        SpawnFrequency = spawnFrequency;
        SpawnCountPerBatch = spawnCountPerBatch;
        NormalizedTimeRange = normalizedTimeRange;
    }
}
