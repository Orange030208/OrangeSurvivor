using UnityEngine;

/// <summary>
/// 通用展示文档数据源。
/// 用于 UI Hover、面板 Presenter 等场景，统一输出 DisplayDocument。
/// </summary>
public interface IDisplayDocumentSource
{
    DisplayDocument BuildDisplayDocument();
}
