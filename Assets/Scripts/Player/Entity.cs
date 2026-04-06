using Unity.VisualScripting;
using UnityEngine;

public interface IEntity
{
    Transform Transform { get; }
    Vector2 Center { get; }

    /// <summary>
    /// 事件总线用的实体运行时唯一ID（单机主线程场景）。
    /// 注意：仅在本次运行期内唯一，不用于存档。
    /// </summary>
    int EventBusId { get; }
}


public abstract class Entity : MonoBehaviour, IEntity
{
    public virtual Transform Transform => transform;
    public virtual Vector2 Center => transform.position;
    public int EventBusId => gameObject.GetInstanceID();
}


public static class EntityExtensions
{
    public static float Distance(this IEntity a, IEntity b)
    {
        return Vector2.Distance(a.Center, b.Center);
    }
}
