using UnityEngine;

public readonly struct AttackHitContext
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public float Range { get; }
    public LayerMask LayerMask { get; }
    public Collider2D OwnerCollider { get; }

    public AttackHitContext(
        Entity attacker,
        Entity target,
        Vector2 origin,
        Vector2 direction,
        float range,
        LayerMask layerMask,
        Collider2D ownerCollider)
    {
        Attacker = attacker;
        Target = target;
        Origin = origin;
        Direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        Range = Mathf.Max(0f, range);
        LayerMask = layerMask;
        OwnerCollider = ownerCollider;
    }
}
