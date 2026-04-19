using UnityEngine;

public readonly struct EnemyMovementContext
{
    public Enemy Enemy { get; }
    public Player TargetPlayer { get; }
    public float AttackDetectionRadius { get; }
    public float DeltaTime { get; }

    public EnemyMovementContext(Enemy enemy, Player targetPlayer, float attackDetectionRadius, float deltaTime)
    {
        Enemy = enemy;
        TargetPlayer = targetPlayer;
        AttackDetectionRadius = attackDetectionRadius;
        DeltaTime = deltaTime;
    }
}
