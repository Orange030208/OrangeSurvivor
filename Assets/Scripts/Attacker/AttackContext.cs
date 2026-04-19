using UnityEngine;

public readonly struct AttackContext
{
    public Entity SourceEntity { get; }
    public Entity TargetEntity { get; }
    public Vector2 AttackOrigin { get; }
    public Vector2 AimDirection { get; }
    public HitSpec HitSpec { get; }

    public AttackContext(Entity sourceEntity, Entity targetEntity, Vector2 attackOrigin, Vector2 aimDirection, HitSpec hitSpec)
    {
        SourceEntity = sourceEntity;
        TargetEntity = targetEntity;
        AttackOrigin = attackOrigin;
        AimDirection = aimDirection.sqrMagnitude > 0f ? aimDirection.normalized : Vector2.zero;
        HitSpec = hitSpec;
    }
}
