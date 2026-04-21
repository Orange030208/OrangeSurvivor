using UnityEngine;

/// <summary>
/// 数值列表中的单条条目。
/// </summary>
public sealed class PropItem
{
    public string Key { get; set; }
    public string Value { get; set; }
    public Sprite Icon { get; set; }
    public float? NumericValue { get; set; }
    public string StyleKey { get; set; }
}
