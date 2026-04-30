using UnityEngine;

[CreateAssetMenu(fileName = "Collider Attack Hit Shape", menuName = ScriptableObjectMenuPaths.COLLIDER_ATTACK_HIT_SHAPE)]
public sealed class ColliderAttackHitShapeSO : AttackHitShapeSO
{
    [SerializeField, Min(0f)] private float extraRadius;

    public override bool Contains(in AttackHitContext context)
    {
        if (context.Target == null || context.OwnerCollider == null)
        {
            return false;
        }

        Vector2 closestPoint = context.OwnerCollider.ClosestPoint(context.Target.Center);
        float allowedDistance = Mathf.Max(0f, context.Range) + extraRadius;
        return Vector2.Distance(closestPoint, context.Target.Center) <= allowedDistance;
    }

    private void OnValidate()
    {
        extraRadius = Mathf.Max(0f, extraRadius);
    }
}
