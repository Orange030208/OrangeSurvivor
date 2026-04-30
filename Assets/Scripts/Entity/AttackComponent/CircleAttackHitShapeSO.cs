using UnityEngine;

[CreateAssetMenu(fileName = "Circle Attack Hit Shape", menuName = ScriptableObjectMenuPaths.CIRCLE_ATTACK_HIT_SHAPE)]
public sealed class CircleAttackHitShapeSO : AttackHitShapeSO
{
    [SerializeField, Min(0f)] private float originForwardOffset;

    public override bool Contains(in AttackHitContext context)
    {
        if (context.Target == null)
        {
            return false;
        }

        Vector2 origin = context.Origin + context.Direction * originForwardOffset;
        return Vector2.Distance(origin, context.Target.Center) <= context.Range;
    }
}
