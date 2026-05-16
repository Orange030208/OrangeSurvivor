using System;

/// <summary>
/// 刷怪候选敌人的语义标签，用于波次配置和运行时刷怪修饰器匹配。
/// </summary>
[Flags]
public enum WaveEnemyTag
{
    None = 0,
    Normal = 1 << 0,
    Elite = 1 << 1,
    Ranged = 1 << 2,
    Fast = 1 << 3,
    BossLike = 1 << 4,
    Special = 1 << 5,
    Heavy = 1 << 6,
    Boss = 1 << 7
}
