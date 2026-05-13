using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private readonly Dictionary<int, Enemy> aliveEnemies = new();
    private readonly HashSet<int> aliveBossIds = new();

    public int AliveEnemyCount => aliveEnemies.Count;
    public int AliveBossCount => aliveBossIds.Count;

    private void OnEnable()
    {
        GameEventBus.Subscribe<EnemyRegisteredEvent>(OnEnemyRegistered);
        GameEventBus.Subscribe<EnemyUnregisteredEvent>(OnEnemyUnregistered);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EnemyRegisteredEvent>(OnEnemyRegistered);
        GameEventBus.Unsubscribe<EnemyUnregisteredEvent>(OnEnemyUnregistered);
    }

    private void OnEnemyRegistered(EnemyRegisteredEvent eventData)
    {
        if (eventData.Enemy == null)
        {
            return;
        }

        int enemyId = eventData.Enemy.GetInstanceID();
        aliveEnemies[enemyId] = eventData.Enemy;
        if (eventData.Role == EnemyRole.Boss)
        {
            aliveBossIds.Add(enemyId);
        }
    }

    private void OnEnemyUnregistered(EnemyUnregisteredEvent eventData)
    {
        if (eventData.Enemy == null)
        {
            return;
        }

        int enemyId = eventData.Enemy.GetInstanceID();
        aliveEnemies.Remove(enemyId);
        aliveBossIds.Remove(enemyId);
    }

    public void DefeatAllTrackedEnemies()
    {
        CancelPendingEnemySpawns();

        Enemy[] enemies = new Enemy[aliveEnemies.Count];
        aliveEnemies.Values.CopyTo(enemies, 0);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
            {
                continue;
            }
            
            enemies[i].DefeatSilently();
        }
    }

    public Enemy[] CreateAliveEnemySnapshot()
    {
        Enemy[] enemies = new Enemy[aliveEnemies.Count];
        aliveEnemies.Values.CopyTo(enemies, 0);
        return enemies;
    }

    public void CancelPendingEnemySpawns()
    {
        CancelPendingEnemySpawnIndicators();
    }

    public void ClearTracking()
    {
        aliveEnemies.Clear();
        aliveBossIds.Clear();
    }

    private static void CancelPendingEnemySpawnIndicators()
    {
        SpawnIndicator[] indicators = FindObjectsByType<SpawnIndicator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] == null)
            {
                continue;
            }

            indicators[i].Cancel();
        }
    }

}
