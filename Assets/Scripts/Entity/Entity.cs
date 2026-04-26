using System;
using UnityEngine;

public interface IEntity
{
    Transform Transform { get; }
    Vector2 Center { get; }
    EntityRenderer EntityRenderer { get; }

    /// <summary>
    /// 事件总线用的实体运行时唯一ID
    /// 仅在本次运行期内唯一，不用于存档。
    /// </summary>
    int EventBusId { get; }
}

public abstract class Entity : MonoBehaviour, IEntity
{
    private EntityComponentBase[] cachedComponents = Array.Empty<EntityComponentBase>();
    private EntityRenderer cachedEntityRenderer;

    public virtual IMovable MoveComponent => IMovable.Empty;
    public virtual Transform Transform => transform;
    public virtual Vector2 Center => transform.position;
    public int EventBusId => gameObject.GetInstanceID();

    public virtual EntityRenderer EntityRenderer
    {
        get
        {
            if (cachedEntityRenderer == null)
            {
                cachedEntityRenderer = GetComponent<EntityRenderer>();
            }

            return cachedEntityRenderer;
        }
    }


    protected void InitializeComponent()
    {
        RefreshComponentCache();
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].Initialize(this);
        }
    }

    protected void DisableAllComponents()
    {
        for (int i = cachedComponents.Length - 1; i >= 0; i--)
        {
            cachedComponents[i].OnDisableComponent();
        }
    }

    protected void EnableAllComponents()
    {
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].OnEnableComponent();
        }
    }

    protected void TickAllComponents()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].OnTick(deltaTime);
        }
    }

    protected void FixedTickAllComponents()
    {
        float deltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].OnFixedTick(deltaTime);
        }
    }

    protected void RefreshComponentCache()
    {
        cachedComponents = GetComponents<EntityComponentBase>();
        Array.Sort(cachedComponents);
    }
}

public static class EntityExtensions
{
    public static float Distance(this IEntity a, IEntity b)
    {
        return Vector2.Distance(a.Center, b.Center);
    }

    public static Entity FindClosestTargetInRange(this Entity self, float searchRange, LayerMask targetLayerMask)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(self.transform.position, searchRange, targetLayerMask);
        Entity closestTarget = null;
        float minDistance = searchRange;
        Vector2 selfCenter = self.transform.position;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].TryGetComponent(out Entity entityChecked))
            {
                continue;
            }

            float distanceToTarget = Vector2.Distance(selfCenter, entityChecked.Center);
            if (distanceToTarget < minDistance)
            {
                closestTarget = entityChecked;
                minDistance = distanceToTarget;
            }
        }

        return closestTarget;
    }
}
