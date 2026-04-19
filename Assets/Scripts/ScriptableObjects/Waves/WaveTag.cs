using System;

/// <summary>
/// 波次语义标签。
/// 只用于表达波次属性和展示语义，不直接作为运行时完成条件判定来源。
/// </summary>
[Flags]
public enum WaveTag
{
    None = 0,
    Normal = 1 << 0,
    Elite = 1 << 1,
    Boss = 1 << 2,
    Event = 1 << 3
}
