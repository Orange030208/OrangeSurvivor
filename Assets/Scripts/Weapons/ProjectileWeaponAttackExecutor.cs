using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 远程攻击执行器：
/// 只做一件事——把已经解析好的攻击上下文，转换成一个实际生成的 Projectile。
/// 它不关心索敌、冷却、序列，也不关心多弹模式；
/// 那些由 RangeWeapon 决定，这里只负责“从哪里发、发什么、带什么上下文”。
/// </summary>
public sealed class ProjectileWeaponAttackExecutor : IWeaponAttackExecutor, IProjectileLauncher
{
    private readonly Projectile projectilePrefab;
    private readonly Transform defaultFiringPoint;
    private readonly Transform[] firingPoints;

    public ProjectileWeaponAttackExecutor(Projectile projectilePrefab, Transform defaultFiringPoint, Transform[] firingPoints = null)
    {
        this.projectilePrefab = projectilePrefab ?? throw new ArgumentNullException(nameof(projectilePrefab), $"{nameof(ProjectileWeaponAttackExecutor)} requires {nameof(projectilePrefab)}.");
        this.defaultFiringPoint = defaultFiringPoint != null
            ? defaultFiringPoint
            : throw new ArgumentNullException(nameof(defaultFiringPoint), $"{nameof(ProjectileWeaponAttackExecutor)} requires {nameof(defaultFiringPoint)}.");
        this.firingPoints = firingPoints;
    }

    public void ExecuteAttack(in WeaponAttackContext context)
    {
        ExecuteAttack(context, ProjectileSpawnPayload.Default);
    }

    /// <summary>
    /// 根据 payload 解析发射点，然后实例化并发射对应投射物。
    /// </summary>
    public void ExecuteAttack(in WeaponAttackContext context, ProjectileSpawnPayload payload)
    {
        Transform firingPoint = ResolveFiringPoint(payload.SpawnPointIndex);
        Projectile projectile = Object.Instantiate(projectilePrefab, firingPoint.position, Quaternion.identity);
        LayerMask targetLayerMask = ResolveTargetLayerMask(context.Weapon);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            context.SourceEntity,
            firingPoint.position,
            context.AimDirection,
            context.HitSpec,
            targetLayerMask,
            payload.SpawnPointIndex,
            payload.ProjectileDefinition,
            payload.BurstId,
            payload.FiringMode,
            payload.PatternConfig));
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {

            throw new ArgumentNullException(nameof(projectile), $"{nameof(ProjectileWeaponAttackExecutor)} requires a valid {nameof(IProjectile)} instance.");
        }

        if (context.ProjectileDefinition != null)
        {
            AudioSfxBridge.RequestPlay(context.ProjectileDefinition.LaunchSfxKey);
            ApplyProjectilePresentation(projectile, context.ProjectileDefinition);
        }

        projectile.Launch(context);
    }

    private static void ApplyProjectilePresentation(IProjectile projectile, ProjectileDefinitionSO projectileDefinition)
    {
        switch (projectileDefinition.TemplateKind)
        {
            case ProjectileTemplateKind.Common:
                projectile.EntityRenderer.SetSprite(projectileDefinition.Icon);
                break;
            default:
                Debug.LogError($"Projectile Template Kind {projectileDefinition.TemplateKind} is not supported.");
                break;
        }

    }

    /// <summary>
    /// 先尝试使用 payload 指定的发射点；没有命中时再回退到默认发射点。
    /// </summary>
    private Transform ResolveFiringPoint(int spawnPointIndex)
    {
        if (firingPoints != null && spawnPointIndex >= 0 && spawnPointIndex < firingPoints.Length && firingPoints[spawnPointIndex] != null)
        {
            return firingPoints[spawnPointIndex];
        }

        return defaultFiringPoint;
    }

    private static LayerMask ResolveTargetLayerMask(Weapon weapon)
    {
        if (weapon == null)
        {
            throw new ArgumentNullException(nameof(weapon), $"{nameof(ProjectileWeaponAttackExecutor)} requires {nameof(Weapon)} to resolve target layer mask.");
        }

        return weapon.TargetLayerMask;
    }
}
