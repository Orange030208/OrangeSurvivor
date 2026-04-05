using UnityEngine;

public interface IEntity
{
    Vector2 Center { get; }

    /// <summary>
    /// 事件总线用的实体运行时唯一ID（单机主线程场景）。
    /// 注意：仅在本次运行期内唯一，不用于存档。
    /// </summary>
    int EventBusId { get; }
}
