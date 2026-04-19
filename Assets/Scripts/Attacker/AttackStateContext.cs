using UnityEngine;

public readonly struct AttackStateContext
{
    public Entity Owner { get; }
    public Entity Target { get; }
    public Transform AttackOrigin { get; }

    public AttackStateContext(Entity owner, Entity target, Transform attackOrigin)
    {
        Owner = owner;
        Target = target;
        AttackOrigin = attackOrigin;
    }
}
