/// <summary>
/// 统一的展示文档构建协议。
/// 用于将业务对象转换为标准化 DisplayDocument。
/// </summary>
public interface IDisplayDocumentBuilder<in TSource>
{
    DisplayDocument Build(TSource source, DisplayContext context = null);
}
