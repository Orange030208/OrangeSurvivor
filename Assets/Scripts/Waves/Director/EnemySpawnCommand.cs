public sealed class EnemySpawnCommand
{
    public EnemySpawnCommand(
        string entryId,
        EnemySO enemyDefinition,
        SpawnRole role,
        WaveEnemyTag enemyTags,
        int spawnCount,
        float unitCost,
        SpawnLocationDefinition spawnRule,
        SpawnReason reason,
        bool consumesBudget,
        string beatId = null)
    {
        EntryId = entryId;
        EnemyDefinition = enemyDefinition;
        Role = role;
        EnemyTags = enemyTags;
        SpawnCount = spawnCount;
        UnitCost = unitCost;
        SpawnRule = spawnRule;
        Reason = reason;
        ConsumesBudget = consumesBudget;
        BeatId = beatId;
    }

    public string EntryId { get; }
    public EnemySO EnemyDefinition { get; }
    public SpawnRole Role { get; }
    public WaveEnemyTag EnemyTags { get; }
    public int SpawnCount { get; }
    public float UnitCost { get; }
    public SpawnLocationDefinition SpawnRule { get; }
    public SpawnReason Reason { get; }
    public bool ConsumesBudget { get; }
    public string BeatId { get; }
    public bool IsScriptedBeat => !string.IsNullOrWhiteSpace(BeatId);
    public bool IsValid => EnemyDefinition != null && SpawnCount > 0 && UnitCost > 0f;
}
