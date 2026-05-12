using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyFactory
{
    private readonly SpawnIndicator spawnIndicatorPrefab;
    private static RunProgressionProfileSO fallbackProgressionProfile;

    public EnemyFactory(SpawnIndicator spawnIndicatorPrefab = null)
    {
        this.spawnIndicatorPrefab = spawnIndicatorPrefab;
    }

    public void Spawn(
        EnemySO enemyData,
        Entity target,
        Vector3 spawnPosition,
        Transform parent,
        RunProgressionSnapshot progressionSnapshot = default,
        WaveEnemyTag enemyTags = WaveEnemyTag.Normal)
    {
        if (enemyData == null)
        {
            throw new MissingReferenceException($"{nameof(EnemyFactory)} requires a non-null {nameof(EnemySO)}.");
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), $"{nameof(EnemyFactory)} requires an explicit non-null {nameof(Entity)} target.");
        }

        Enemy template = enemyData.prefab;
        if (template == null)
        {
            throw new MissingReferenceException($"{nameof(EnemyFactory)} cannot resolve prefab from {enemyData.name}.");
        }

        if (spawnIndicatorPrefab != null)
        {
            SpawnIndicator indicator = Object.Instantiate(spawnIndicatorPrefab, spawnPosition, Quaternion.identity, parent);
            indicator.PlayAndSpawn(template.gameObject, spawnPosition, Quaternion.identity, parent, spawnedObject =>
            {
                if (!spawnedObject.TryGetComponent(out Enemy spawnedEnemy))
                {
                    throw new MissingReferenceException($"{nameof(EnemyFactory)} expected spawned object to contain {nameof(Enemy)}.");
                }

                ApplyEnemyData(spawnedEnemy, enemyData, target, progressionSnapshot, enemyTags);
            });
            return;
        }

        Enemy enemy = Object.Instantiate(template, spawnPosition, Quaternion.identity, parent);
        ApplyEnemyData(enemy, enemyData, target, progressionSnapshot, enemyTags);
    }

    private static void ApplyEnemyData(
        Enemy enemy,
        EnemySO enemyData,
        Entity target,
        RunProgressionSnapshot progressionSnapshot,
        WaveEnemyTag enemyTags)
    {
        RunProgressionSnapshot snapshot = progressionSnapshot.WaveNumber > 0
            ? progressionSnapshot
            : RunProgressionRuntime.CurrentSnapshot;
        RunProgressionEnemyScale scale = ResolveEnemyScale(enemyData, snapshot, enemyTags);
        enemy.Configure(enemyData, target, RunProgressionEnemyScaling.BuildModifiers(scale));
    }

    private static RunProgressionEnemyScale ResolveEnemyScale(
        EnemySO enemyData,
        RunProgressionSnapshot snapshot,
        WaveEnemyTag enemyTags)
    {
        if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider) &&
            provider.RunProgressionProfile != null)
        {
            return provider.RunProgressionProfile.EvaluateEnemyScale(snapshot, enemyData, enemyTags);
        }

        fallbackProgressionProfile ??= RunProgressionProfileSO.CreateRuntimeDefault();
        return fallbackProgressionProfile.EvaluateEnemyScale(snapshot, enemyData, enemyTags);
    }
}
