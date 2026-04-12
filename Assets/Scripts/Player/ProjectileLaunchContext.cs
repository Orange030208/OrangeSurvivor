using UnityEngine;

/// <summary>
/// 子弹发射上下文：
/// 这是投射物离开发射器时拿到的初始参数快照。
/// Bullet 及其子类会基于它决定：
/// - 从哪里出发；
/// - 朝哪里飞；
/// - 造成多少伤害；
/// - 使用哪份弹射物定义；
/// - 属于哪种模式。
/// </summary>
public readonly struct ProjectileLaunchContext
{
    public Vector2 SpawnPosition { get; }
    public Vector2 Direction { get; }
    public ResolvedWeaponHit Hit { get; }
    public int SpawnPointIndex { get; }
    public ProjectileDefinitionSO ProjectileDefinition { get; }
    public int BurstId { get; }
    public ProjectileFiringMode FiringMode { get; }
    public ProjectilePatternConfig PatternConfig { get; }

    public ProjectileLaunchContext(
        Vector2 spawnPosition,
        Vector2 direction,
        ResolvedWeaponHit hit,
        int spawnPointIndex = 0,
        ProjectileDefinitionSO projectileDefinition = null,
        int burstId = 0,
        ProjectileFiringMode firingMode = ProjectileFiringMode.Default,
        ProjectilePatternConfig patternConfig = default)
    {
        SpawnPosition = spawnPosition;
        Direction = direction.normalized;
        Hit = hit;
        SpawnPointIndex = Mathf.Max(0, spawnPointIndex);
        ProjectileDefinition = projectileDefinition;
        BurstId = Mathf.Max(0, burstId);
        FiringMode = firingMode;
        PatternConfig = patternConfig.Equals(default(ProjectilePatternConfig)) ? ProjectilePatternConfig.Default : patternConfig;
    }
}
