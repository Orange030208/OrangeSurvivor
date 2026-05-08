using UnityEngine;

public struct EnemyRegisteredEvent : IGameEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyRegisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}

public struct EnemyUnregisteredEvent : IGameEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyUnregisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}
