using UnityEngine;

public sealed class EnemyMovementContext
{
    public EnemyMovementContext(Enemy owner, Entity target, Movement movement, float deltaTime, float attackDetectionRadius)
    {
        Owner = owner;
        Target = target;
        Movement = movement;
        DeltaTime = deltaTime;
        AttackDetectionRadius = Mathf.Max(0f, attackDetectionRadius);
    }

    public Enemy Owner { get; }
    public Entity Target { get; }
    public Movement Movement { get; }
    public float DeltaTime { get; }
    public float AttackDetectionRadius { get; }
}
