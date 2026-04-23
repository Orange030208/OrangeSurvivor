using System;
using UnityEngine;

/// <summary>
/// 单条刷怪计划。
/// 描述某个敌人定义在一段归一化时间区间内，以什么频率和批次数量进行生成。
/// </summary>
[Serializable]
public struct WaveSpawnPlan
{
    private const int MIN_SPAWN_COUNT_PER_BATCH = 1;
    private const float MIN_SPAWN_FREQUENCY = 0.01f;

    [SerializeField] private EnemySO enemyDefinition;
    [SerializeField] private float spawnFrequency;
    [SerializeField] private int spawnCountPerBatch;
    [SerializeField] private Vector2 normalizedTimeRange;

    public EnemySO EnemyDefinition => enemyDefinition;
    public float SpawnFrequency => Mathf.Max(MIN_SPAWN_FREQUENCY, spawnFrequency);
    public int SpawnCountPerBatch => Mathf.Max(MIN_SPAWN_COUNT_PER_BATCH, spawnCountPerBatch);
    public Vector2 NormalizedTimeRange => normalizedTimeRange;

    public WaveSpawnPlan(EnemySO enemyDefinition, float spawnFrequency, int spawnCountPerBatch, Vector2 normalizedTimeRange)
    {
        this.enemyDefinition = enemyDefinition;
        this.spawnFrequency = spawnFrequency;
        this.spawnCountPerBatch = spawnCountPerBatch;
        this.normalizedTimeRange = normalizedTimeRange;
    }
}
