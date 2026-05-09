using System;
using UnityEngine;

/// <summary>
/// 刷怪包中的单个敌人生成项；刷怪包负责描述组合，实际生成仍由波次执行服务处理。
/// </summary>
[Serializable]
public struct WaveSpawnPackEntry
{
    private const int MIN_SPAWN_COUNT = 1;

    [SerializeField] private EnemySO enemyDefinition;
    [SerializeField, Min(MIN_SPAWN_COUNT)] private int spawnCount;
    [SerializeField] private WaveEnemyTag enemyTags;
    [SerializeField] private bool overrideTags;

    public EnemySO EnemyDefinition => enemyDefinition;
    public int SpawnCount => Mathf.Max(MIN_SPAWN_COUNT, spawnCount);
    public bool OverrideTags => overrideTags;
    public WaveEnemyTag EnemyTags => enemyTags == WaveEnemyTag.None ? WaveEnemyTag.Normal : enemyTags;
    public bool IsValid => enemyDefinition != null && SpawnCount > 0;

    public WaveSpawnPackEntry(
        EnemySO enemyDefinition,
        int spawnCount,
        WaveEnemyTag enemyTags = WaveEnemyTag.Normal,
        bool overrideTags = true)
    {
        this.enemyDefinition = enemyDefinition;
        this.spawnCount = Mathf.Max(MIN_SPAWN_COUNT, spawnCount);
        this.enemyTags = enemyTags == WaveEnemyTag.None ? WaveEnemyTag.Normal : enemyTags;
        this.overrideTags = overrideTags;
    }
}
