using System;
using System.Collections;
using UnityEngine;

public delegate void ProjectileLaunchHandler(IProjectile projectile, in ProjectileLaunchContext context);

public readonly struct ProjectilePatternEmissionContext
{
    public IProjectileLauncher Launcher { get; }
    public Func<Entity> ResolveSourceEntity { get; }
    public Func<HitSpec> BuildHitSpec { get; }
    public Func<int, WeaponSpawnPointPose> ResolveSpawnPointPose { get; }
    public Func<WeaponSpawnPointPose, Vector2> ResolveAimDirection { get; }
    public Func<int> ResolvePierceCount { get; }
    public Func<LayerMask> ResolveTargetLayerMask { get; }
    public Func<float> ResolveMaxTravelDistance { get; }
    public ProjectileLaunchHandler LaunchProjectile { get; }
    public Func<IEnumerator, Coroutine> StartCoroutine { get; }

    public ProjectilePatternEmissionContext(
        IProjectileLauncher launcher,
        Func<Entity> resolveSourceEntity,
        Func<HitSpec> buildHitSpec,
        Func<int, WeaponSpawnPointPose> resolveSpawnPointPose,
        Func<WeaponSpawnPointPose, Vector2> resolveAimDirection,
        Func<int> resolvePierceCount,
        Func<LayerMask> resolveTargetLayerMask,
        Func<float> resolveMaxTravelDistance,
        ProjectileLaunchHandler launchProjectile,
        Func<IEnumerator, Coroutine> startCoroutine)
    {
        Launcher = launcher;
        ResolveSourceEntity = resolveSourceEntity;
        BuildHitSpec = buildHitSpec;
        ResolveSpawnPointPose = resolveSpawnPointPose;
        ResolveAimDirection = resolveAimDirection;
        ResolvePierceCount = resolvePierceCount;
        ResolveTargetLayerMask = resolveTargetLayerMask;
        ResolveMaxTravelDistance = resolveMaxTravelDistance;
        LaunchProjectile = launchProjectile;
        StartCoroutine = startCoroutine;
    }

    public void Validate()
    {
        if (Launcher == null)
        {
            throw new ArgumentNullException(nameof(Launcher));
        }

        if (ResolveSourceEntity == null)
        {
            throw new ArgumentNullException(nameof(ResolveSourceEntity));
        }

        if (BuildHitSpec == null)
        {
            throw new ArgumentNullException(nameof(BuildHitSpec));
        }

        if (ResolveSpawnPointPose == null)
        {
            throw new ArgumentNullException(nameof(ResolveSpawnPointPose));
        }

        if (ResolveAimDirection == null)
        {
            throw new ArgumentNullException(nameof(ResolveAimDirection));
        }

        if (ResolvePierceCount == null)
        {
            throw new ArgumentNullException(nameof(ResolvePierceCount));
        }

        if (ResolveTargetLayerMask == null)
        {
            throw new ArgumentNullException(nameof(ResolveTargetLayerMask));
        }

        if (ResolveMaxTravelDistance == null)
        {
            throw new ArgumentNullException(nameof(ResolveMaxTravelDistance));
        }

        if (LaunchProjectile == null)
        {
            throw new ArgumentNullException(nameof(LaunchProjectile));
        }

        if (StartCoroutine == null)
        {
            throw new ArgumentNullException(nameof(StartCoroutine));
        }
    }
}

public sealed class ProjectilePatternEmitter
{
    private int activeBurstId = -1;

    public void ResetBurstState()
    {
        activeBurstId = -1;
    }

    public void Emit(WeaponSequenceProjectileDefinition projectileConfig, in ProjectilePatternEmissionContext context)
    {
        context.Validate();

        switch (projectileConfig.FiringMode)
        {
            case ProjectileFiringMode.Burst:
                TryStartBurst(projectileConfig, context);
                break;
            case ProjectileFiringMode.Spread:
                FireSpread(projectileConfig, context);
                break;
            case ProjectileFiringMode.Nova:
                FireNova(projectileConfig, context);
                break;
            default:
                FireSingle(projectileConfig, null, context);
                break;
        }
    }

    private IEnumerator CreateBurstRoutine(
        WeaponSequenceProjectileDefinition projectileConfig,
        ProjectilePatternEmissionContext context)
    {
        context.Validate();

        int burstCount = projectileConfig.PatternConfig.BurstCount;
        float burstInterval = projectileConfig.PatternConfig.BurstInterval;

        for (int i = 0; i < burstCount; i++)
        {
            FireSingle(projectileConfig, null, context);
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        activeBurstId = -1;
    }

    private void TryStartBurst(
        WeaponSequenceProjectileDefinition projectileConfig,
        ProjectilePatternEmissionContext context)
    {
        if (activeBurstId == projectileConfig.BurstId)
        {
            return;
        }

        activeBurstId = projectileConfig.BurstId;
        context.StartCoroutine(CreateBurstRoutine(projectileConfig, context));
    }

    private void FireSpread(
        WeaponSequenceProjectileDefinition projectileConfig,
        ProjectilePatternEmissionContext context)
    {
        int spreadCount = projectileConfig.PatternConfig.SpreadCount;
        if (spreadCount <= 1)
        {
            FireSingle(projectileConfig, null, context);
            return;
        }

        float spreadAngle = projectileConfig.PatternConfig.SpreadAngle;
        float step = spreadCount > 1 ? (spreadAngle * 2f) / (spreadCount - 1) : 0f;
        for (int i = 0; i < spreadCount; i++)
        {
            float angle = -spreadAngle + (step * i);
            FireSingle(projectileConfig, angle, context);
        }
    }

    private void FireNova(
        WeaponSequenceProjectileDefinition projectileConfig,
        ProjectilePatternEmissionContext context)
    {
        int novaCount = projectileConfig.PatternConfig.NovaCount;
        for (int i = 0; i < novaCount; i++)
        {
            float angle = 360f / novaCount * i;
            FireSingle(projectileConfig, angle, context);
        }
    }

    private void FireSingle(
        WeaponSequenceProjectileDefinition projectileConfig,
        float? angleOffset,
        ProjectilePatternEmissionContext context)
    {
        WeaponSpawnPointPose origin = context.ResolveSpawnPointPose(projectileConfig.SpawnPointIndex);
        Entity sourceEntity = context.ResolveSourceEntity();
        HitSpec hitSpec = context.BuildHitSpec();
        Vector2 aimDirection = context.ResolveAimDirection(origin);
        if (angleOffset.HasValue)
        {
            aimDirection = (Quaternion.Euler(0f, 0f, angleOffset.Value) * aimDirection).normalized;
        }

        ExecuteProjectileAttack(sourceEntity, origin, aimDirection, hitSpec, projectileConfig, context);
    }

    private void ExecuteProjectileAttack(
        Entity sourceEntity,
        WeaponSpawnPointPose origin,
        Vector2 aimDirection,
        HitSpec hitSpec,
        WeaponSequenceProjectileDefinition projectileConfig,
        ProjectilePatternEmissionContext context)
    {
        Projectile projectile = ProjectileFactory.CreateProjectile(
            projectileConfig.ProjectileDefinition,
            origin.Position,
            Quaternion.identity);
        context.LaunchProjectile(projectile, new ProjectileLaunchContext(
            context.Launcher,
            sourceEntity,
            origin.Position,
            aimDirection,
            hitSpec,
            context.ResolveTargetLayerMask(),
            projectileConfig.ProjectileDefinition,
            context.ResolvePierceCount(),
            projectileConfig.SpawnPointIndex,
            projectileConfig.BurstId,
            projectileConfig.FiringMode,
            projectileConfig.PatternConfig,
            maxTravelDistance: context.ResolveMaxTravelDistance()));
    }
}
