using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyFactory
{
    private readonly SpawnIndicator spawnIndicatorPrefab;

    public EnemyFactory(SpawnIndicator spawnIndicatorPrefab = null)
    {
        this.spawnIndicatorPrefab = spawnIndicatorPrefab;
    }

    public void Spawn(EnemySO enemyData, Entity target, Vector3 spawnPosition, Transform parent)
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

                ApplyEnemyData(spawnedEnemy, enemyData, target);
            });
            return;
        }

        Enemy enemy = Object.Instantiate(template, spawnPosition, Quaternion.identity, parent);
        ApplyEnemyData(enemy, enemyData, target);
    }

    private static void ApplyEnemyData(Enemy enemy, EnemySO enemyData, Entity target)
    {
        enemy.Configure(enemyData, target);
    }
}
