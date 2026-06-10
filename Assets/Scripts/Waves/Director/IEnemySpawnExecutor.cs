public readonly struct SpawnedEnemyHandle
{
    public SpawnedEnemyHandle(Enemy enemy, string entryId, SpawnRole role, float unitCost)
    {
        Enemy = enemy;
        EntryId = entryId;
        Role = role;
        UnitCost = unitCost;
    }

    public Enemy Enemy { get; }
    public string EntryId { get; }
    public SpawnRole Role { get; }
    public float UnitCost { get; }
}

public interface IEnemySpawnExecutor
{
    int Execute(
        EnemySpawnCommand command,
        WaveDirectorExecutionContext context,
        SpawnPositionResolver defaultResolver,
        System.Action<SpawnedEnemyHandle> onSpawned);
}
