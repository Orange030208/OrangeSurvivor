using UnityEngine;

/// <summary>
/// 远程攻击执行器：
/// 只做一件事——把已经解析好的攻击上下文，转换成一个实际生成的 Bullet。
/// 它不关心索敌、冷却、序列，也不关心多弹模式；
/// 那些由 RangeWeapon 决定，这里只负责“从哪里发、发什么、带什么上下文”。
/// </summary>
public sealed class ProjectileWeaponAttackExecutor : IWeaponAttackExecutor
{
    private readonly Bullet bulletPrefab;
    private readonly Transform defaultFiringPoint;
    private readonly Transform[] firingPoints;

    public ProjectileWeaponAttackExecutor(Bullet bulletPrefab, Transform defaultFiringPoint, Transform[] firingPoints = null)
    {
        this.bulletPrefab = bulletPrefab;
        this.defaultFiringPoint = defaultFiringPoint;
        this.firingPoints = firingPoints;
    }

    public void ExecuteAttack(in WeaponAttackContext context)
    {
        ExecuteAttack(context, ProjectileSpawnPayload.Default);
    }

    /// <summary>
    /// 根据 payload 解析发射点，然后实例化并发射对应子弹。
    /// </summary>
    public void ExecuteAttack(in WeaponAttackContext context, ProjectileSpawnPayload payload)
    {
        Transform firingPoint = ResolveFiringPoint(payload.SpawnPointIndex);
        if (bulletPrefab == null || firingPoint == null)
        {
            return;
        }

        Bullet bullet = Object.Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        bullet.Launch(new ProjectileLaunchContext(
            context.SourceEntity,
            firingPoint.position,
            context.AimDirection,
            context.HitSpec,
            payload.SpawnPointIndex,
            payload.ProjectileDefinition,
            payload.BurstId,
            payload.FiringMode,
            payload.PatternConfig));
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
}
