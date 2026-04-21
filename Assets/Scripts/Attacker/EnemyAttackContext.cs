using UnityEngine;

public sealed class EnemyAttackContext
{
    public EnemyAttackContext(Attacker attacker, Entity owner, Entity target, float attackDetectionRadius, float deltaTime)
    {
        Attacker = attacker;
        Owner = owner;
        Target = target;
        AttackDetectionRadius = Mathf.Max(0f, attackDetectionRadius);
        DeltaTime = deltaTime;
    }

    public Attacker Attacker { get; }
    public Entity Owner { get; }
    public Entity Target { get; }
    public float AttackDetectionRadius { get; }
    public float DeltaTime { get; }
}
