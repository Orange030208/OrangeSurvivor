public sealed class WaveEnemySpawnCandidate
{
    public EnemySO EnemyDefinition { get; set; }
    public float Weight { get; set; }
    public WaveEnemyTag Tags { get; set; }

    public WaveEnemySpawnCandidate(EnemySO enemyDefinition, float weight, WaveEnemyTag tags)
    {
        EnemyDefinition = enemyDefinition;
        Weight = weight;
        Tags = tags;
    }

    public bool IsValid => EnemyDefinition != null && Weight > 0f;
}
