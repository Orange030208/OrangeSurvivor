using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCombatController : MonoBehaviour
{
    private Enemy owner;
    private Attacker attacker;
    private Entity targetEntity;
    private EnemyMovementDefinitionSO movementDefinition;
    private AttackDefinitionSO attackDefinition;
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
        movementDefinition = runtimeMovementDefinition;
        attackDefinition = runtimeAttackDefinition;
        attackDetectionRadius = Mathf.Max(0f, detectionRadius);

        if (attacker == null)
        {
            return;
        }

        if (attackDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(EnemyCombatController)} requires {nameof(AttackDefinitionSO)} before configuring {nameof(Attacker)}.");
        }

        attacker.Configure(targetEntity, attackDefinition, attackDetectionRadius);
    }

    private void Update()
    {
        if (owner == null || targetEntity == null)
        {
            return;
        }

        bool hasAttacked = attacker != null && attacker.Tick(Time.deltaTime);
        if (!hasAttacked)
        {
            TickMovement(Time.deltaTime);
        }
    }

    private void TickMovement(float deltaTime)
    {
        if (owner.MoveComponent is not Movement movement || movementDefinition == null)
        {
            return;
        }

        switch (movementDefinition.MovementType)
        {
            case EnemyMovementType.ChaseIntoContact:
                movement.FollowTarget(targetEntity, deltaTime, 0f);
                break;
            case EnemyMovementType.StopAtAttackRange:
                movement.FollowTarget(targetEntity, deltaTime, attackDetectionRadius);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(movementDefinition), movementDefinition.MovementType, "Unsupported enemy movement type.");
        }
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
