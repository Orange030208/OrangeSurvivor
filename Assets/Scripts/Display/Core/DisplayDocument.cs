using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 标准化展示文档根对象。
/// 业务对象应先被转换为文档，再由具体 UI 按支持的 Block 类型渲染。
/// </summary>
public sealed class DisplayDocument
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Footer { get; set; }
    public Sprite Icon { get; set; }
    public IReadOnlyList<DisplayBlock> Blocks { get; set; } = System.Array.Empty<DisplayBlock>();
}
