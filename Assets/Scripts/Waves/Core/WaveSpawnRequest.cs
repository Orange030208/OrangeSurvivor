public sealed class WaveSpawnRequest
{
    public EnemySO EnemyDefinition { get; set; }
    public int SpawnCount { get; set; }
    public WaveEnemyTag EnemyTags { get; set; }
    public string SourceTrackId { get; }
    public int SegmentIndex { get; }
    public bool IsCancelled { get; set; }

    public WaveSpawnRequest(
        EnemySO enemyDefinition,
        int spawnCount,
        WaveEnemyTag enemyTags,
        string sourceTrackId,
        int segmentIndex)
    {
        EnemyDefinition = enemyDefinition;
        SpawnCount = spawnCount;
        EnemyTags = enemyTags;
        SourceTrackId = sourceTrackId;
        SegmentIndex = segmentIndex;
    }

    public bool IsValid => !IsCancelled && EnemyDefinition != null && SpawnCount > 0;
}
