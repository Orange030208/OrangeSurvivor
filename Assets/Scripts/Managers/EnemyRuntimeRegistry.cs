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
        GameEventBus.Subscribe<EnemyRegisteredEvent>(OnEnemyRuntimeRegistered);
        GameEventBus.Subscribe<EnemyUnregisteredEvent>(OnEnemyRuntimeUnregistered);
        GameEventBus.Subscribe<DefeatAllTrackedEnemiesRequestedEvent>(OnDefeatAllTrackedEnemiesRequested);
        GameEventBus.Subscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<EnemyRegisteredEvent>(OnEnemyRuntimeRegistered);
        GameEventBus.Unsubscribe<EnemyUnregisteredEvent>(OnEnemyRuntimeUnregistered);
        GameEventBus.Unsubscribe<DefeatAllTrackedEnemiesRequestedEvent>(OnDefeatAllTrackedEnemiesRequested);
        GameEventBus.Unsubscribe<ResetWavesRequestedEvent>(OnResetWavesRequested);
    }

    private void OnEnemyRuntimeRegistered(EnemyRegisteredEvent eventData)
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

    private void OnEnemyRuntimeUnregistered(EnemyUnregisteredEvent eventData)
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
            
            //TODO:后续改为播放动画
            Destroy(enemies[i].gameObject);
        }
    }

    private void OnResetWavesRequested()
    {
        aliveEnemies.Clear();
        aliveBossIds.Clear();
    }
}