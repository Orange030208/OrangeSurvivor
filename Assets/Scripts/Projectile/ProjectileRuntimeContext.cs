using UnityEngine;

/// <summary>
/// 弹射物运行时上下文。
/// 作为发射快照和 Unity 组件引用的集中入口，避免运动/命中模块彼此直接耦合。
/// </summary>
public readonly struct ProjectileRuntimeContext
{
    public ProjectileRuntimeContext(
        Projectile projectile,
        ProjectileLaunchContext launchContext,
        ProjectileDefinitionSO definition,
        Transform transform,
        Rigidbody2D rigidbody,
        Collider2D collider,
        LayerMask obstacleLayerMask)
    {
        Projectile = projectile;
        LaunchContext = launchContext;
        Definition = definition;
        Transform = transform;
        Rigidbody = rigidbody;
        Collider = collider;
        ObstacleLayerMask = obstacleLayerMask;
    }

    public Projectile Projectile { get; }
    public ProjectileLaunchContext LaunchContext { get; }
    public ProjectileDefinitionSO Definition { get; }
    public Transform Transform { get; }
    public Rigidbody2D Rigidbody { get; }
    public Collider2D Collider { get; }
    public LayerMask TargetLayerMask => LaunchContext.TargetLayerMask;
    public LayerMask ObstacleLayerMask { get; }
    public float MaxTravelDistance => LaunchContext.MaxTravelDistance;
}

public enum ProjectileContactKind
{
    Target = 0,
    Obstacle = 1,
    LifetimeExpired = 2,
}

public readonly struct ProjectileContact
{
    public ProjectileContact(
        ProjectileContactKind kind,
        Collider2D collider,
        HealthComponent healthComponent,
        Vector2 impactPosition)
    {
        Kind = kind;
        Collider = collider;
        HealthComponent = healthComponent;
        ImpactPosition = impactPosition;
    }

    public ProjectileContactKind Kind { get; }
    public Collider2D Collider { get; }
    public HealthComponent HealthComponent { get; }
    public Vector2 ImpactPosition { get; }
}

public readonly struct ProjectileImpactResult
{
    public static ProjectileImpactResult None => new(false, false, Vector2.zero);

    public static ProjectileImpactResult Despawn(bool spawnDefaultImpactVfx, Vector2 impactPosition)
    {
        return new ProjectileImpactResult(true, spawnDefaultImpactVfx, impactPosition);
    }

    public static ProjectileImpactResult KeepAlive(bool spawnDefaultImpactVfx, Vector2 impactPosition)
    {
        return new ProjectileImpactResult(false, spawnDefaultImpactVfx, impactPosition);
    }

    private ProjectileImpactResult(bool shouldDespawn, bool spawnDefaultImpactVfx, Vector2 impactPosition)
    {
        ShouldDespawn = shouldDespawn;
        SpawnDefaultImpactVfx = spawnDefaultImpactVfx;
        ImpactPosition = impactPosition;
    }

    public bool ShouldDespawn { get; }
    public bool SpawnDefaultImpactVfx { get; }
    public Vector2 ImpactPosition { get; }
}

public readonly struct ProjectileLifetimeResult
{
    public static ProjectileLifetimeResult Active => new(false, Vector2.zero);

    public static ProjectileLifetimeResult Expired(Vector2 impactPosition)
    {
        return new ProjectileLifetimeResult(true, impactPosition);
    }

    private ProjectileLifetimeResult(bool isExpired, Vector2 impactPosition)
    {
        IsExpired = isExpired;
        ImpactPosition = impactPosition;
    }

    public bool IsExpired { get; }
    public Vector2 ImpactPosition { get; }
}
