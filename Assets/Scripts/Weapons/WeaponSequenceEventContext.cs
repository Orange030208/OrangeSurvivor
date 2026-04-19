using UnityEngine;

/// <summary>
/// 序列事件上下文：
/// WeaponMotionSequencePlayer 在命中某个事件关键帧时，会构造一份上下文并交给 WeaponSequenceBridge。
/// 武器逻辑层只消费这里暴露的标准化数据，而不直接读取关键帧数组。
/// </summary>
public readonly struct WeaponSequenceEventContext
{
    /// <summary>
    /// 当前事件的类型，例如开命中窗口、发射弹射物、播放特效等。
    /// </summary>
    public WeaponSequenceEventType EventType { get; }

    /// <summary>
    /// 事件键。
    /// - 对 OpenHitWindow / CloseHitWindow：表示命中窗口编号；
    /// - 对 SpawnProjectile / PlaySfx / PlayVfx：表示 WeaponDataSO 对应配置列表的下标。
    /// </summary>
    public int EventKey { get; }

    public WeaponSequenceEventContext(WeaponSequenceEventType eventType, int eventKey)
    {
        EventType = eventType;
        EventKey = Mathf.Max(0, eventKey);
    }

    public static WeaponSequenceEventContext CreateWindowEvent(WeaponSequenceEventType eventType, int eventKey)
    {
        return new WeaponSequenceEventContext(eventType, eventKey);
    }

    public static WeaponSequenceEventContext CreateProjectileEvent(int eventKey)
    {
        return new WeaponSequenceEventContext(WeaponSequenceEventType.SpawnProjectile, eventKey);
    }

    public static WeaponSequenceEventContext CreateSimpleEvent(WeaponSequenceEventType eventType, int eventKey = 0)
    {
        return new WeaponSequenceEventContext(eventType, eventKey);
    }
}

/// <summary>
/// 发射事件的载荷。
/// 它描述“这一次 SpawnProjectile 应该怎么发”，但不直接生成弹射物；
/// 真正的实例化仍然由具体武器 + 对应执行器负责。
/// </summary>
public readonly struct ProjectileSpawnPayload
{
    public static ProjectileSpawnPayload Default => new ProjectileSpawnPayload(0, null, 0, ProjectileFiringMode.Default, ProjectilePatternConfig.Default);

    /// <summary>
    /// 使用哪个发射点。
    /// 0 通常表示默认枪口；更大的索引用于多枪口或多炮口武器。
    /// </summary>
    public int SpawnPointIndex { get; }

    /// <summary>
    /// 直接引用要发射的弹射物定义资源。
    /// 这样序列不再依赖 WeaponDataSO 的列表顺序，配置会稳定得多。
    /// </summary>
    public ProjectileDefinitionSO ProjectileDefinition { get; }

    /// <summary>
    /// Burst 逻辑的分组 id。
    /// 当前主要用于避免同一 burst 重复启动；后续也可以扩展成更复杂的 burst 状态机键值。
    /// </summary>
    public int BurstId { get; }

    /// <summary>
    /// 本次发射所使用的模式：单发、散射、连发、Nova 等。
    /// </summary>
    public ProjectileFiringMode FiringMode { get; }

    /// <summary>
    /// 多弹模式所需的数量、角度、间隔配置。
    /// </summary>
    public ProjectilePatternConfig PatternConfig { get; }

    public ProjectileSpawnPayload(int spawnPointIndex, ProjectileDefinitionSO projectileDefinition, int burstId, ProjectileFiringMode firingMode, ProjectilePatternConfig patternConfig)
    {
        SpawnPointIndex = Mathf.Max(0, spawnPointIndex);
        ProjectileDefinition = projectileDefinition;
        BurstId = Mathf.Max(0, burstId);
        FiringMode = firingMode;
        PatternConfig = patternConfig;
    }
}

/// <summary>
/// 多弹模式参数。
/// 当前支持：Spread / Burst / Nova。
/// 后续如果需要 Charged 的额外参数，建议单独再加配置结构，而不是继续往这里硬塞字段。
/// </summary>
public readonly struct ProjectilePatternConfig
{
    public static ProjectilePatternConfig Default => new ProjectilePatternConfig(3, 12f, 3, 0.06f, 8);

    public int SpreadCount { get; }
    public float SpreadAngle { get; }
    public int BurstCount { get; }
    public float BurstInterval { get; }
    public int NovaCount { get; }

    public ProjectilePatternConfig(int spreadCount, float spreadAngle, int burstCount, float burstInterval, int novaCount)
    {
        SpreadCount = Mathf.Max(1, spreadCount);
        SpreadAngle = Mathf.Max(0f, spreadAngle);
        BurstCount = Mathf.Max(1, burstCount);
        BurstInterval = Mathf.Max(0f, burstInterval);
        NovaCount = Mathf.Max(1, novaCount);
    }
}

/// <summary>
/// 发射模式枚举。
/// 当前 Charged 仍然只是占位语义，后续如果做蓄力弹，需要继续补运行时实现。
/// </summary>
public enum ProjectileFiringMode
{
    Default,
    Spread,
    Burst,
    Charged,
    Nova
}
