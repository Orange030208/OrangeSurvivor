using UnityEngine;

//原始输入
public readonly struct HitRequest
{
    public Entity Source { get; }
    public Entity Target { get; }
    public HitSpec Spec { get; }
    public Vector2 HitPoint { get; }
    public HitSourceKind SourceKind { get; }
    public string SourceId { get; }

    public HitRequest(Entity source, Entity target, HitSpec spec, Vector2 hitPoint, HitSourceKind sourceKind, string sourceId)
    {
        Source = source;
        Target = target;
        Spec = spec;
        HitPoint = hitPoint;
        SourceKind = sourceKind;
        SourceId = sourceId;
    }
}
