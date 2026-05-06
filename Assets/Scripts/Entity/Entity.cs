using System;
using UnityEditor;
using UnityEngine;

public interface IEntity
{
    Collider2D EntityCollider { get; }
    Transform Transform { get; }
    Vector2 Center { get; }
    EntityRenderer EntityRenderer { get; }
    bool IsRuntimeEnabled { get; }

    /// <summary>
    /// 仅在本次运行期内唯一，不用于存档。
    /// </summary>
    string RuntimeId { get; }

    void EnableRuntime();
    void DisableRuntime();
}

public abstract class Entity : MonoBehaviour, IEntity
{
    private EntityComponentBase[] cachedComponents = Array.Empty<EntityComponentBase>();
    private EntityRenderer cachedEntityRenderer;
    private Collider2D cachedCollider;

    private string runtimeId;
    private bool isRuntimeEnabled = true;

    public virtual Collider2D EntityCollider
    {
        get
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }

            return cachedCollider;
        }
        protected set => cachedCollider = value;
    }

    public virtual IMovable MoveComponent => IMovable.Empty;
    public virtual Transform Transform => transform;

    public virtual Vector2 Center =>
        EntityCollider != null
            ? EntityCollider.bounds.center
            : (Vector2)transform.position;

    public bool IsRuntimeEnabled => isRuntimeEnabled;

    public string RuntimeId
    {
        get
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                runtimeId = $"Entity_{gameObject.GetInstanceID()}_{Guid.NewGuid():N}";
            }

            return runtimeId;
        }
    }

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
        protected set => cachedEntityRenderer = value;
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
        if (!isRuntimeEnabled)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].OnTick(deltaTime);
        }
    }

    protected void FixedTickAllComponents()
    {
        if (!isRuntimeEnabled)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < cachedComponents.Length; i++)
        {
            cachedComponents[i].OnFixedTick(deltaTime);
        }
    }

    public virtual void EnableRuntime()
    {
        isRuntimeEnabled = true;
    }

    public virtual void DisableRuntime()
    {
        isRuntimeEnabled = false;
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

    public static Vector2 GetClosestPointTo(this Entity target, Vector2 point)
    {
        if (target == null)
        {
            return point;
        }

        Collider2D targetCollider = target.EntityCollider;
        return targetCollider != null
            ? targetCollider.ClosestPoint(point)
            : target.Center;
    }

    public static float DistanceToCollider(this Entity target, Vector2 point)
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        return Vector2.Distance(point, target.GetClosestPointTo(point));
    }

    public static bool IsColliderWithinRange(this Entity target, Vector2 point, float range)
    {
        if (target == null)
        {
            return false;
        }

        float clampedRange = Mathf.Max(0f, range);
        Vector2 closestPoint = target.GetClosestPointTo(point);
        return (closestPoint - point).sqrMagnitude <= clampedRange * clampedRange;
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

            float distanceToTarget = entityChecked.DistanceToCollider(selfCenter);
            if (distanceToTarget <= minDistance)
            {
                closestTarget = entityChecked;
                minDistance = distanceToTarget;
            }
        }

        return closestTarget;
    }
}
