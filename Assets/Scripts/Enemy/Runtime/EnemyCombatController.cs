using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCombatController : MonoBehaviour
{
    private Enemy owner;
    private MoveBase movement;
    private AttackBase attacker;
    private Entity targetEntity;
    private float attackDetectionRadius;

    public void Initialize(Enemy enemy, MoveBase movementComponent, AttackBase attackComponent)
    {
        owner = enemy ?? throw new ArgumentNullException(nameof(enemy));
        movement = movementComponent;
        attacker = attackComponent;

        Transform attackOrigin = ResolveAttackOriginTransform();
        if (attacker != null)
        {
            attacker.Initialize(owner, attackOrigin);
        }
    }

    public void Configure(Entity target, float detectionRadius)
    {
        targetEntity = target;
        attackDetectionRadius = Mathf.Max(0f, detectionRadius);
    }

    private void Update()
    {
        if (owner == null || targetEntity == null)
        {
            return;
        }

        bool hasAttacked = attacker != null && attacker.enabled && attacker.Tick(targetEntity, attackDetectionRadius, Time.deltaTime);
        if (!hasAttacked && movement != null && movement.enabled)
        {
            movement.Tick(owner, targetEntity, Time.deltaTime, attackDetectionRadius);
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
