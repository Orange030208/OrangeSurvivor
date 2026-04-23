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
    private bool attackEnabled = true;
    private bool allowMoveWhileAttacking;

    public MoveBase ActiveMovement => movement;
    public AttackBase ActiveAttack => attacker;
    public Entity TargetEntity => targetEntity;
    public float AttackDetectionRadius => attackDetectionRadius;
    public bool AttackEnabled => attackEnabled;
    public bool AllowMoveWhileAttacking => allowMoveWhileAttacking;

    public void Initialize(Enemy enemy, MoveBase movementComponent, AttackBase attackComponent)
    {
        owner = enemy ?? throw new ArgumentNullException(nameof(enemy));
        SetActiveMovement(movementComponent);
        SetActiveAttack(attackComponent);
    }

    public void Configure(Entity target, float detectionRadius)
    {
        targetEntity = target;
        attackDetectionRadius = Mathf.Max(0f, detectionRadius);
    }

    public void SetActiveMovement(MoveBase movementComponent)
    {
        if (movement == movementComponent)
        {
            if (movement != null)
            {
                movement.enabled = true;
            }
            return;
        }

        if (movement != null)
        {
            movement.StopImmediately();
            movement.enabled = false;
        }

        movement = movementComponent;
        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    public void SetActiveAttack(AttackBase attackComponent)
    {
        if (attacker == attackComponent)
        {
            if (attacker != null)
            {
                attacker.enabled = true;
                if (owner != null)
                {
                    attacker.Initialize(owner, ResolveAttackOriginTransform());
                }
            }
            return;
        }

        if (attacker != null)
        {
            attacker.enabled = false;
        }

        attacker = attackComponent;
        if (attacker != null)
        {
            attacker.enabled = true;
            if (owner != null)
            {
                attacker.Initialize(owner, ResolveAttackOriginTransform());
            }
        }
    }

    public void SetAttackEnabled(bool enabled)
    {
        attackEnabled = enabled;
    }

    public void SetAllowMoveWhileAttacking(bool enabled)
    {
        allowMoveWhileAttacking = enabled;
    }

    private void Update()
    {
        if (owner == null || targetEntity == null)
        {
            return;
        }

        bool hasAttacked = attackEnabled && attacker != null && attacker.enabled && attacker.Tick(targetEntity, attackDetectionRadius, Time.deltaTime);
        bool canMoveThisFrame = movement != null && movement.enabled && (allowMoveWhileAttacking || !hasAttacked);
        if (canMoveThisFrame)
        {
            movement.EnableMovement();
            movement.Tick(owner, targetEntity, Time.deltaTime, attackDetectionRadius);
            return;
        }

        if (movement != null)
        {
            movement.StopImmediately();
        }
    }

    private Transform ResolveAttackOriginTransform()
    {
        Transform explicitChildOrigin = transform.Find("AttackOrigin");
        if (explicitChildOrigin != null)
        {
            return explicitChildOrigin;
        }

        Transform legacyShootingPoint = transform.Find("Shooting Point");
        if (legacyShootingPoint != null)
        {
            return legacyShootingPoint;
        }

        return transform;
    }
}
