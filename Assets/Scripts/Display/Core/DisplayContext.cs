/// <summary>
/// 构建展示文档时的上下文参数。
/// 第一阶段仅保留最小集合，后续按真实需求扩展。
/// </summary>
public sealed class DisplayContext
{
    public string ViewKey { get; set; }
    public bool IsCompact { get; set; }
    public bool ShowDebugInfo { get; set; }

    public static DisplayContext Default { get; } = new();
}
