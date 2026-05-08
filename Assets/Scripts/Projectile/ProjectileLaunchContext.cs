using UnityEngine;

/// <summary>
/// 子弹发射上下文：
/// 这是投射物离开发射器时拿到的初始参数快照。
/// Bullet 及其子类会基于它决定：
/// - 从哪里出发；
/// - 朝哪里飞；
/// - 造成多少伤害；
/// - 使用哪份弹射物定义；
/// - 属于哪种模式；
/// - 应该命中哪些目标层。
/// </summary>
public readonly struct ProjectileLaunchContext
{
    public IProjectileLauncher Launcher { get; }
    public Entity Source { get; }
    public Weapon SourceWeapon { get; }
    public Vector2 SpawnPosition { get; }
    public Vector2 Direction { get; }
    public HitSpec HitSpec { get; }
    /// <summary>
    /// 表示改弹射物从哪个发射槽位发射的,默认为0
    /// </summary>
    public int SpawnPointIndex { get; }
    public ProjectileDefinitionSO ProjectileDefinition { get; }
    public int PierceCount { get; }
    /// <summary>
    ///  Burst 连发的分组编号，用来防止同一个 burst 在一次攻击序列里被重复触发多次。
    /// </summary>
    public int BurstId { get; }
    public ProjectileFiringMode FiringMode { get; }
    public ProjectilePatternConfig PatternConfig { get; }
    public LayerMask TargetLayerMask { get; }

    public ProjectileLaunchContext(
        IProjectileLauncher launcher,
        Entity source,
        Vector2 spawnPosition,
        Vector2 direction,
        HitSpec hitSpec,
        LayerMask targetLayerMask,
        ProjectileDefinitionSO projectileDefinition = null,
        int pierceCount = 0,
        int spawnPointIndex = 0,
        int burstId = 0,
        ProjectileFiringMode firingMode = ProjectileFiringMode.Default,
        ProjectilePatternConfig patternConfig = default,
        Weapon sourceWeapon = null)
    {
        Launcher = launcher;
        Source = source;
        SourceWeapon = sourceWeapon != null ? sourceWeapon : launcher as Weapon;
        SpawnPosition = spawnPosition;
        Direction = direction.normalized;
        HitSpec = hitSpec;
        TargetLayerMask = targetLayerMask;
        PierceCount = Mathf.Max(0, pierceCount);
        SpawnPointIndex = Mathf.Max(0, spawnPointIndex);
        ProjectileDefinition = projectileDefinition;
        BurstId = Mathf.Max(0, burstId);
        FiringMode = firingMode;
        PatternConfig = patternConfig.Equals(default(ProjectilePatternConfig)) ? ProjectilePatternConfig.Default : patternConfig;
    }
}
