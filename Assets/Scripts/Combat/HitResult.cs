using UnityEngine;

public readonly struct HitResult
{
    public Entity Source { get; }
    public Entity Target { get; }
    public float FinalDamage { get; }
    public Vector2 HitPoint { get; }
    public bool IsCritical { get; }
    public bool IsDodged { get; }
    public bool IsBlocked { get; }
    public bool IsCancelled { get; }
    public HitSourceKind SourceKind { get; }
    public string SourceId { get; }

    public HitResult(Entity source, Entity target, float finalDamage, Vector2 hitPoint, bool isCritical, bool isDodged, bool isBlocked, bool isCancelled, HitSourceKind sourceKind, string sourceId)
    {
        Source = source;
        Target = target;
        FinalDamage = Mathf.Max(0f, finalDamage);
        HitPoint = hitPoint;
        IsCritical = isCritical;
        IsDodged = isDodged;
        IsBlocked = isBlocked;
        IsCancelled = isCancelled;
        SourceKind = sourceKind;
        SourceId = sourceId;
    }

    public HitResult WithFinalDamage(float finalDamage)
    {
        return new HitResult(Source, Target, finalDamage, HitPoint, IsCritical, IsDodged, IsBlocked, IsCancelled, SourceKind, SourceId);
    }
}
