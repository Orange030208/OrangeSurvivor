using UnityEngine;

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
        ExecuteAttack(context, new ProjectileSpawnPayload(0, 0, 0, ProjectileFiringMode.Default));
    }

    public void ExecuteAttack(in WeaponAttackContext context, ProjectileSpawnPayload payload)
    {
        Transform firingPoint = ResolveFiringPoint(payload.SpawnPointIndex);
        if (bulletPrefab == null || firingPoint == null)
        {
            return;
        }

        Vector2 direction = ResolveDirection(context, payload.FiringMode);
        Bullet bullet = Object.Instantiate(bulletPrefab, firingPoint.position, Quaternion.identity);
        bullet.Launch(new ProjectileLaunchContext(
            firingPoint.position,
            direction,
            context.Hit,
            payload.SpawnPointIndex,
            payload.ProjectileVariantIndex,
            payload.BurstId,
            payload.FiringMode));
    }

    private Transform ResolveFiringPoint(int spawnPointIndex)
    {
        if (firingPoints != null && spawnPointIndex >= 0 && spawnPointIndex < firingPoints.Length && firingPoints[spawnPointIndex] != null)
        {
            return firingPoints[spawnPointIndex];
        }

        return defaultFiringPoint;
    }

    private Vector2 ResolveDirection(in WeaponAttackContext context, ProjectileFiringMode firingMode)
    {
        switch (firingMode)
        {
            case ProjectileFiringMode.Spread:
                return Quaternion.Euler(0f, 0f, 12f) * context.AimDirection;
            case ProjectileFiringMode.Nova:
                return context.Origin.right;
            default:
                return context.AimDirection;
        }
    }
}
