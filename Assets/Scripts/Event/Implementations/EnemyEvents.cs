using UnityEngine;

public struct EnemyRegisteredEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyRegisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}

public struct EnemyUnregisteredEvent
{
    public Enemy Enemy;
    public EnemyRole Role;

    public EnemyUnregisteredEvent(Enemy enemy, EnemyRole role)
    {
        Enemy = enemy;
        Role = role;
    }
}
