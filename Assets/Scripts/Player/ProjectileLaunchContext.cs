using UnityEngine;

public readonly struct ProjectileLaunchContext
{
    public Vector2 SpawnPosition { get; }
    public Vector2 Direction { get; }
    public ResolvedWeaponHit Hit { get; }
    public int SpawnPointIndex { get; }
    public int ProjectileVariantIndex { get; }
    public int BurstId { get; }
    public ProjectileFiringMode FiringMode { get; }

    public ProjectileLaunchContext(
        Vector2 spawnPosition,
        Vector2 direction,
        ResolvedWeaponHit hit,
        int spawnPointIndex = 0,
        int projectileVariantIndex = 0,
        int burstId = 0,
        ProjectileFiringMode firingMode = ProjectileFiringMode.Default)
    {
        SpawnPosition = spawnPosition;
        Direction = direction.normalized;
        Hit = hit;
        SpawnPointIndex = Mathf.Max(0, spawnPointIndex);
        ProjectileVariantIndex = Mathf.Max(0, projectileVariantIndex);
        BurstId = Mathf.Max(0, burstId);
        FiringMode = firingMode;
    }
}
