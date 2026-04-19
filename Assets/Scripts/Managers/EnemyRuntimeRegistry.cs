using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntimeRegistry : MonoBehaviour
{
    private readonly Dictionary<int, Enemy> aliveEnemies = new();
    private readonly HashSet<int> aliveBossIds = new();

    public int AliveEnemyCount => aliveEnemies.Count;
    public int AliveBossCount => aliveBossIds.Count;

    private void OnEnable()
    {
        GameEventBus.Subscribe<EnemyRuntimeRegisteredEvent>(OnEnemyRuntimeRegistered);
        GameEventBus.Subscribe<EnemyRuntimeUnregisteredEvent>(OnEnemyRuntimeUnregistered);
        GameEventBus.Subscribe<DefeatAllTrackedEnemiesRequestedEvent>(OnDefeatAllTrackedEnemiesRequested);
        GameEventBus.Subscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EnemyRuntimeRegisteredEvent>(OnEnemyRuntimeRegistered);
        GameEventBus.Unsubscribe<EnemyRuntimeUnregisteredEvent>(OnEnemyRuntimeUnregistered);
        GameEventBus.Unsubscribe<DefeatAllTrackedEnemiesRequestedEvent>(OnDefeatAllTrackedEnemiesRequested);
        GameEventBus.Unsubscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
    }

    private void OnEnemyRuntimeRegistered(EnemyRuntimeRegisteredEvent eventData)
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

    private void OnEnemyRuntimeUnregistered(EnemyRuntimeUnregisteredEvent eventData)
    {
        if (eventData.Enemy == null)
        {
            return;
        }

        int enemyId = eventData.Enemy.GetInstanceID();
        aliveEnemies.Remove(enemyId);
        aliveBossIds.Remove(enemyId);
    }

    private void OnDefeatAllTrackedEnemiesRequested()
    {
        Enemy[] enemies = new Enemy[aliveEnemies.Count];
        aliveEnemies.Values.CopyTo(enemies, 0);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
            {
                continue;
            }

            enemies[i].PassAwayAfterWave();
        }
    }

    private void OnResetWavesRequested()
    {
        aliveEnemies.Clear();
        aliveBossIds.Clear();
    }
}