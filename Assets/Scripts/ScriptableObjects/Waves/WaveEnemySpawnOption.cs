using System;
using UnityEngine;

/// <summary>
/// 单个刷怪候选项。权重只影响同一轨道内的随机选择概率。
/// </summary>
[Serializable]
public struct WaveEnemySpawnOption
{
    private const float MIN_WEIGHT = 0f;

    [SerializeField] private EnemySO enemyDefinition;
    [SerializeField] private float weight;
    [SerializeField] private WaveEnemyTag tags;

    public EnemySO EnemyDefinition => enemyDefinition;
    public float Weight => Mathf.Max(MIN_WEIGHT, weight);
    public WaveEnemyTag Tags => tags;

    public WaveEnemySpawnOption(EnemySO enemyDefinition, float weight, WaveEnemyTag tags)
    {
        this.enemyDefinition = enemyDefinition;
        this.weight = weight;
        this.tags = tags;
    }
}
