using UnityEngine;

public struct EnemyRuntimeRegisteredEvent : IGameEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyRuntimeRegisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}

public struct EnemyRuntimeUnregisteredEvent : IGameEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyRuntimeUnregisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}

public struct DefeatAllTrackedEnemiesRequestedEvent : IGameEvent
{
}
