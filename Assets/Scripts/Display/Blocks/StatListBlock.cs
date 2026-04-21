using System.Collections.Generic;

/// <summary>
/// 属性/数值列表块。
/// 用于角色属性、装备词条、面板数值等结构化数值展示场景。
/// </summary>
public sealed class StatListBlock : DisplayBlock
{
    public IReadOnlyList<StatItem> Items { get; set; } = System.Array.Empty<StatItem>();
}
