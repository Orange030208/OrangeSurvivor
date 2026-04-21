/// <summary>
/// 所有展示块的抽象基类。
/// 根文档尽量保持稳定，新增展示能力时优先扩展新的 Block 类型。
/// </summary>
public abstract class DisplayBlock
{
    public string BlockId { get; set; }
    public string Header { get; set; }
    public int Order { get; set; }
}
