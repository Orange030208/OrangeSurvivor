using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCombatController : MonoBehaviour
{
    private Enemy owner;
    private Attacker attacker;
    private Entity targetEntity;
    private IEnemyMovementStrategy movementStrategy;
    private IEnemyAttackStrategy attackStrategy;
    private float attackDetectionRadius;

    public void Initialize(Enemy enemy, Attacker runtimeAttacker)
    {
        owner = enemy ?? throw new ArgumentNullException(nameof(enemy));
        attacker = runtimeAttacker;

        Transform attackOrigin = ResolveAttackOriginTransform();
        if (attacker != null)
        {
            attacker.Initialize(owner, attackOrigin);
        }
    }

    public void Configure(Entity target, EnemyMovementDefinitionSO runtimeMovementDefinition, AttackDefinitionSO runtimeAttackDefinition, float detectionRadius)
    {
        targetEntity = target;
        movementStrategy = runtimeMovementDefinition != null ? runtimeMovementDefinition.CreateRuntimeStrategy() : null;
        attackStrategy = runtimeAttackDefinition != null ? runtimeAttackDefinition.CreateRuntimeStrategy() : null;
        attackDetectionRadius = Mathf.Max(0f, detectionRadius);

        if (runtimeAttackDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(EnemyCombatController)} requires {nameof(AttackDefinitionSO)} before configuring combat.");
        }

        attacker?.EnsureInitialized();
    }

    private void Update()
    {
        if (owner == null || targetEntity == null)
        {
            return;
        }

        bool hasAttacked = TickAttack(Time.deltaTime);
        if (!hasAttacked)
        {
            TickMovement(Time.deltaTime);
        }
    }

    private bool TickAttack(float deltaTime)
    {
        if (attackStrategy == null)
        {
            return false;
        }

        EnemyAttackContext context = new EnemyAttackContext(attacker, owner, targetEntity, attackDetectionRadius, deltaTime);
        return attackStrategy.Tick(context);
    }

    private void TickMovement(float deltaTime)
    {
        if (owner.MoveComponent is not Movement movement || movementStrategy == null)
        {
            return;
        }

        EnemyMovementContext context = new EnemyMovementContext(owner, targetEntity, movement, deltaTime, attackDetectionRadius);
        movementStrategy.Tick(context);
    }

    private Transform ResolveAttackOriginTransform()
    {
        Transform explicitChildOrigin = transform.Find("AttackOrigin");
        if (explicitChildOrigin != null)
        {
            return explicitChildOrigin;
        }

        return transform;
    }
}
