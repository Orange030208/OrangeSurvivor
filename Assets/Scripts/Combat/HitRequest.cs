using UnityEngine;

//原始输入
public readonly struct HitRequest
{
    private const float MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity Source { get; }
    public Entity Target { get; }
    public HitSpec Spec { get; }
    public Vector2 HitPoint { get; }
    public bool HasKnockbackDirection { get; }
    public Vector2 KnockbackDirection { get; }
    public HitSourceKind SourceKind { get; }
    public string SourceId { get; }

    public HitRequest(Entity source, Entity target, HitSpec spec, Vector2 hitPoint, HitSourceKind sourceKind, string sourceId)
        : this(source, target, spec, hitPoint, false, Vector2.zero, sourceKind, sourceId)
    {
    }

    public HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        Vector2 knockbackDirection,
        HitSourceKind sourceKind,
        string sourceId)
        : this(source, target, spec, hitPoint, true, knockbackDirection, sourceKind, sourceId)
    {
    }

    private HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        bool hasKnockbackDirection,
        Vector2 knockbackDirection,
        HitSourceKind sourceKind,
        string sourceId)
    {
        Source = source;
        Target = target;
        Spec = spec;
        HitPoint = hitPoint;
        HasKnockbackDirection = hasKnockbackDirection &&
            knockbackDirection.sqrMagnitude > MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE;
        KnockbackDirection = HasKnockbackDirection ? knockbackDirection.normalized : Vector2.zero;
        SourceKind = sourceKind;
        SourceId = sourceId;
    }
}
