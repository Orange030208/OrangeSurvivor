using UnityEngine;

public sealed class UnityEnemySpawnExecutor : IEnemySpawnExecutor
{
    private readonly EnemyFactory enemyFactory;

    public UnityEnemySpawnExecutor(EnemyFactory enemyFactory)
    {
        this.enemyFactory = enemyFactory;
    }

    public int Execute(
        EnemySpawnCommand command,
        WaveDirectorExecutionContext context,
        SpawnPositionResolver defaultResolver,
        System.Action<SpawnedEnemyHandle> onSpawned)
    {
        if (command == null || !command.IsValid || context.SpawnAnchor == null)
        {
            return 0;
        }

        SpawnPositionResolver resolver = command.SpawnRule != null
            ? SpawnPositionResolver.FromDefinition(command.SpawnRule)
            : defaultResolver;
        if (resolver == null)
        {
            return 0;
        }

        int spawnedCount = 0;
        Player player = context.Player;
        for (int i = 0; i < command.SpawnCount; i++)
        {
            SpawnContext positionContext = new(context.SpawnAnchor, context.ElapsedTime, context.WaveIndex);
            if (!resolver.TryResolve(positionContext, command.EnemyDefinition, out Vector3 spawnPosition))
            {
                Debug.LogWarning($"[{nameof(UnityEnemySpawnExecutor)}] Skipped spawning {command.EnemyDefinition.name} because no safe spawn position could be resolved.");
                continue;
            }

            try
            {
                Enemy enemy = enemyFactory.Spawn(
                    command.EnemyDefinition,
                    player,
                    spawnPosition,
                    context.SpawnParent,
                    context.ProgressionSnapshot,
                    command.EnemyTags);
                if (enemy == null)
                {
                    continue;
                }

                spawnedCount++;
                onSpawned?.Invoke(new SpawnedEnemyHandle(enemy, command.EntryId, command.Role, command.UnitCost));
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(UnityEnemySpawnExecutor)}] Failed to spawn {command.EnemyDefinition.name} on wave {context.WaveNumber}.",
                    context.SpawnParent);
                Debug.LogException(exception, context.SpawnParent);
            }
        }

        return spawnedCount;
    }
}
