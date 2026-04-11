using UnityEngine;

public readonly struct WeaponSequenceEventContext
{
    public WeaponSequenceEventType EventType { get; }
    public int WindowId { get; }
    public ProjectileSpawnPayload ProjectileSpawnPayload { get; }

    public WeaponSequenceEventContext(WeaponSequenceEventType eventType, int windowId, ProjectileSpawnPayload projectileSpawnPayload)
    {
        EventType = eventType;
        WindowId = Mathf.Max(0, windowId);
        ProjectileSpawnPayload = projectileSpawnPayload;
    }

    public static WeaponSequenceEventContext CreateWindowEvent(WeaponSequenceEventType eventType, int windowId)
    {
        return new WeaponSequenceEventContext(eventType, windowId, ProjectileSpawnPayload.Default);
    }

    public static WeaponSequenceEventContext CreateProjectileEvent(ProjectileSpawnPayload projectileSpawnPayload)
    {
        return new WeaponSequenceEventContext(WeaponSequenceEventType.SpawnProjectile, 0, projectileSpawnPayload);
    }

    public static WeaponSequenceEventContext CreateSimpleEvent(WeaponSequenceEventType eventType)
    {
        return new WeaponSequenceEventContext(eventType, 0, ProjectileSpawnPayload.Default);
    }
}

public readonly struct ProjectileSpawnPayload
{
    public static ProjectileSpawnPayload Default => new ProjectileSpawnPayload(0, 0, 0, ProjectileFiringMode.Default);

    public int SpawnPointIndex { get; }
    public int ProjectileVariantIndex { get; }
    public int BurstId { get; }
    public ProjectileFiringMode FiringMode { get; }

    public ProjectileSpawnPayload(int spawnPointIndex, int projectileVariantIndex, int burstId, ProjectileFiringMode firingMode)
    {
        SpawnPointIndex = Mathf.Max(0, spawnPointIndex);
        ProjectileVariantIndex = Mathf.Max(0, projectileVariantIndex);
        BurstId = Mathf.Max(0, burstId);
        FiringMode = firingMode;
    }
}

public enum ProjectileFiringMode
{
    Default,
    Spread,
    Burst,
    Charged,
    Nova
}
