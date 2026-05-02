using UnityEngine;

public readonly struct HitResult
{
    private const float MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity Source { get; }
    public Entity Target { get; }
    public float FinalDamage { get; }
    public float KnockbackStrength { get; }
    public Vector2 HitPoint { get; }
    /// <summary>
    /// 伤害源或攻击发起位置快照，不随 Source 后续移动而改变。
    /// </summary>
    public Vector2 SourcePosition { get; }
    public bool HasKnockbackDirection { get; }
    public Vector2 KnockbackDirection { get; }
    public bool IsCritical { get; }
    public bool IsDodged { get; }
    public bool IsBlocked { get; }
    public bool IsCancelled { get; }
    public HitSourceKind SourceKind { get; }
    public string SourceId { get; }

    public HitResult(Entity source, Entity target, float finalDamage, Vector2 hitPoint, bool isCritical, bool isDodged, bool isBlocked, bool isCancelled, HitSourceKind sourceKind, string sourceId, Vector2 sourcePosition)
        : this(source, target, finalDamage, 0f, hitPoint, false, Vector2.zero, isCritical, isDodged, isBlocked, isCancelled, sourceKind, sourceId, sourcePosition)
    {
    }

    public HitResult(Entity source, Entity target, float finalDamage, float knockbackStrength, Vector2 hitPoint, bool isCritical, bool isDodged, bool isBlocked, bool isCancelled, HitSourceKind sourceKind, string sourceId, Vector2 sourcePosition)
        : this(source, target, finalDamage, knockbackStrength, hitPoint, false, Vector2.zero, isCritical, isDodged, isBlocked, isCancelled, sourceKind, sourceId, sourcePosition)
    {
    }

    public HitResult(
        Entity source,
        Entity target,
        float finalDamage,
        float knockbackStrength,
        Vector2 hitPoint,
        bool hasKnockbackDirection,
        Vector2 knockbackDirection,
        bool isCritical,
        bool isDodged,
        bool isBlocked,
        bool isCancelled,
        HitSourceKind sourceKind,
        string sourceId,
        Vector2 sourcePosition)
    {
        Source = source;
        Target = target;
        FinalDamage = Mathf.Max(0f, finalDamage);
        KnockbackStrength = Mathf.Max(0f, knockbackStrength);
        HitPoint = hitPoint;
        SourcePosition = sourcePosition;
        HasKnockbackDirection = hasKnockbackDirection &&
            knockbackDirection.sqrMagnitude > MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE;
        KnockbackDirection = HasKnockbackDirection ? knockbackDirection.normalized : Vector2.zero;
        IsCritical = isCritical;
        IsDodged = isDodged;
        IsBlocked = isBlocked;
        IsCancelled = isCancelled;
        SourceKind = sourceKind;
        SourceId = sourceId;
    }

    public HitResult WithFinalDamage(float finalDamage)
    {
        return new HitResult(
            Source,
            Target,
            finalDamage,
            KnockbackStrength,
            HitPoint,
            HasKnockbackDirection,
            KnockbackDirection,
            IsCritical,
            IsDodged,
            IsBlocked,
            IsCancelled,
            SourceKind,
            SourceId,
            SourcePosition);
    }
}
