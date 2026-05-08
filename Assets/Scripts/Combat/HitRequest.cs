using UnityEngine;

//原始输入
public readonly struct HitRequest
{
    private const float MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity Source { get; }
    public Entity Target { get; }
    public HitSpec Spec { get; }
    public Vector2 HitPoint { get; }
    /// <summary>
    /// 伤害源或攻击发起位置快照，不随 Source 后续移动而改变。
    /// </summary>
    public Vector2 SourcePosition { get; }
    public bool HasKnockbackDirection { get; }
    public Vector2 KnockbackDirection { get; }
    public HitSourceKind SourceKind { get; }
    public Weapon SourceWeapon { get; }

    public HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        HitSourceKind sourceKind,
        Vector2 sourcePosition,
        Weapon sourceWeapon = null)
        : this(
            source,
            target,
            spec,
            hitPoint,
            false,
            Vector2.zero,
            sourcePosition,
            sourceKind,
            sourceWeapon)
    {
    }

    public HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        Vector2 knockbackDirection,
        HitSourceKind sourceKind,
        Vector2 sourcePosition,
        Weapon sourceWeapon = null)
        : this(
            source,
            target,
            spec,
            hitPoint,
            true,
            knockbackDirection,
            sourcePosition,
            sourceKind,
            sourceWeapon)
    {
    }

    private HitRequest(
        Entity source,
        Entity target,
        HitSpec spec,
        Vector2 hitPoint,
        bool hasKnockbackDirection,
        Vector2 knockbackDirection,
        Vector2 sourcePosition,
        HitSourceKind sourceKind,
        Weapon sourceWeapon)
    {
        Source = source;
        Target = target;
        Spec = spec;
        HitPoint = hitPoint;
        SourcePosition = sourcePosition;
        HasKnockbackDirection = hasKnockbackDirection &&
            knockbackDirection.sqrMagnitude > MIN_KNOCKBACK_DIRECTION_SQR_MAGNITUDE;
        KnockbackDirection = HasKnockbackDirection ? knockbackDirection.normalized : Vector2.zero;
        SourceKind = sourceKind;
        SourceWeapon = sourceWeapon;
    }
}
