using UnityEngine;

public abstract class AttackHitShapeSO : ScriptableObject
{
    public abstract bool Contains(in AttackHitContext context);
}
