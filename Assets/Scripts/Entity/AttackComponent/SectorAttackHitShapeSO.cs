using UnityEngine;

[CreateAssetMenu(fileName = "Sector Attack Hit Shape", menuName = ScriptableObjectMenuPaths.SECTOR_ATTACK_HIT_SHAPE)]
public sealed class SectorAttackHitShapeSO : AttackHitShapeSO
{
    [SerializeField, Range(1f, 360f)] private float angle = 90f;
    [SerializeField, Min(0f)] private float originForwardOffset;

    public float Angle => angle;

    public override bool Contains(in AttackHitContext context)
    {
        if (context.Target == null)
        {
            return false;
        }

        Vector2 origin = context.Origin + context.Direction * originForwardOffset;
        Vector2 toTarget = context.Target.Center - origin;
        if (toTarget.sqrMagnitude > context.Range * context.Range)
        {
            return false;
        }

        return Vector2.Angle(context.Direction, toTarget) <= angle * 0.5f;
    }

    private void OnValidate()
    {
        angle = Mathf.Clamp(angle, 1f, 360f);
        originForwardOffset = Mathf.Max(0f, originForwardOffset);
    }
}
