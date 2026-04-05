using UnityEngine;

public abstract class Entity : MonoBehaviour, IEntity
{
    public abstract Vector2 Center { get; }

    // Unity 内置运行时实例ID：获取成本低，适合作为事件总线 key。
    public int EventBusId => gameObject.GetInstanceID();
}
