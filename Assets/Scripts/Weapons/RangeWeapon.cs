using System.Collections;
using UnityEngine;

public class RangeWeapon : Weapon
{
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Transform[] additionalShootingPoints;
    [SerializeField] private Bullet[] projectileVariants;
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;
    [SerializeField] private WeaponSequenceBridge sequenceBridge;

    private Enemy pendingTarget;
    private int activeBurstId = -1;

    protected override void Awake()
    {
        base.Awake();

        if (sequenceBridge == null)
        {
            sequenceBridge = GetComponentInChildren<WeaponSequenceBridge>();
        }

        if (attackSequence == null && WeaponData != null)
        {
            attackSequence = WeaponData.AttackSequence;
        }

        if (sequenceBridge != null)
        {
            sequenceBridge.SequenceEventTriggered += OnSequenceEventTriggered;
            sequenceBridge.SequenceCompleted += FinishAttackSequence;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        FinishAttackSequence();
        sequenceBridge?.Stop(true);
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (sequenceBridge == null)
        {
            return;
        }

        sequenceBridge.SequenceEventTriggered -= OnSequenceEventTriggered;
        sequenceBridge.SequenceCompleted -= FinishAttackSequence;
    }

    protected override bool CanStartAttack()
    {
        return !IsAttacking && (sequenceBridge == null || !sequenceBridge.IsPlaying);
    }

    protected override void BeginAttack(Enemy target)
    {
        IsAttacking = true;
        pendingTarget = target;
        activeBurstId = -1;

        if (sequenceBridge != null && attackSequence != null)
        {
            sequenceBridge.Play(attackSequence);
            return;
        }

        FireProjectiles(ProjectileSpawnPayload.Default);
        FinishAttackSequence();
    }

    private void OnSequenceEventTriggered(WeaponSequenceEventContext eventContext)
    {
        switch (eventContext.EventType)
        {
            case WeaponSequenceEventType.SpawnProjectile:
                FireProjectiles(eventContext.ProjectileSpawnPayload);
                break;
            case WeaponSequenceEventType.PlaySfx:
                break;
            case WeaponSequenceEventType.PlayVfx:
                break;
        }
    }

    private void FireProjectiles(ProjectileSpawnPayload payload)
    {
        if (pendingTarget == null)
        {
            return;
        }

        switch (payload.FiringMode)
        {
            case ProjectileFiringMode.Burst:
                TryStartBurst(payload);
                break;
            case ProjectileFiringMode.Spread:
                FireSpread(payload);
                break;
            case ProjectileFiringMode.Nova:
                FireNova(payload);
                break;
            default:
                FireSingle(payload, Vector2.zero);
                break;
        }
    }

    private void TryStartBurst(ProjectileSpawnPayload payload)
    {
        if (activeBurstId == payload.BurstId)
        {
            return;
        }

        activeBurstId = payload.BurstId;
        StartCoroutine(BurstRoutine(payload));
    }

    private IEnumerator BurstRoutine(ProjectileSpawnPayload payload)
    {
        const int burstCount = 3;
        const float burstInterval = 0.06f;

        for (int i = 0; i < burstCount; i++)
        {
            if (pendingTarget == null)
            {
                break;
            }

            FireSingle(payload, Vector2.zero);
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        activeBurstId = -1;
    }

    private void FireSpread(ProjectileSpawnPayload payload)
    {
        FireSingle(payload, Quaternion.Euler(0f, 0f, -12f) * Vector2.right);
        FireSingle(payload, Vector2.zero);
        FireSingle(payload, Quaternion.Euler(0f, 0f, 12f) * Vector2.right);
    }

    private void FireNova(ProjectileSpawnPayload payload)
    {
        const int projectileCount = 8;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 360f / projectileCount * i;
            Vector2 overrideDirection = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            FireSingle(payload, overrideDirection);
        }
    }

    private void FireSingle(ProjectileSpawnPayload payload, Vector2 overrideDirection)
    {
        ProjectileWeaponAttackExecutor executor = BuildExecutor(payload.ProjectileVariantIndex);
        if (executor == null)
        {
            return;
        }

        WeaponAttackContext context = BuildAttackContext(pendingTarget, ResolvePrimaryOrigin());
        if (overrideDirection != Vector2.zero)
        {
            context = new WeaponAttackContext(context.Weapon, context.Origin, context.Target, overrideDirection.normalized, context.Stats, context.Hit);
        }

        executor.ExecuteAttack(context, payload);
    }

    private ProjectileWeaponAttackExecutor BuildExecutor(int projectileVariantIndex)
    {
        if (projectileVariants == null || projectileVariants.Length == 0)
        {
            return null;
        }

        int variantIndex = Mathf.Clamp(projectileVariantIndex, 0, projectileVariants.Length - 1);
        Bullet projectilePrefab = projectileVariants[variantIndex];
        if (projectilePrefab == null)
        {
            return null;
        }

        return new ProjectileWeaponAttackExecutor(projectilePrefab, shootingPoint, additionalShootingPoints);
    }

    private Transform ResolvePrimaryOrigin()
    {
        return shootingPoint != null ? shootingPoint : transform;
    }

    private void FinishAttackSequence()
    {
        pendingTarget = null;
        activeBurstId = -1;
        CompleteAttackCycle();
    }
}
