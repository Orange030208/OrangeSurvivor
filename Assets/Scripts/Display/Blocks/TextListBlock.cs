using System.Collections.Generic;

/// <summary>
/// 纯文本描述列表块。
/// 用于特性说明、提示说明、补充文本等线性文本展示场景。
/// </summary>
public sealed class TextListBlock : DisplayBlock
{
    public IReadOnlyList<TextLineItem> Items { get; set; } = System.Array.Empty<TextLineItem>();
}
