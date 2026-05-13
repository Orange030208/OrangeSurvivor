using UnityEngine;

public class HitCalcModifier : IHitModifier
{
    public int HitPriority => HitModifierPriority.Finally;
    public HitModifierTiming HitModifierTiming => HitModifierTiming.Deal;

    public void ModifyHit(HitContext hitContext)
    {
        if (hitContext == null)
        {
            return;
        }

        if (hitContext.IsCancelled || hitContext.IsDodged || hitContext.IsBlocked)
        {
            hitContext.Damage = 0f;
            return;
        }

        float damage = PropValueUtility.ClampNonNegative(hitContext.Damage);
        if (hitContext.IsCritical)
        {
            damage *= hitContext.CritMultiplier;
        }

        hitContext.Damage = damage * PropValueUtility.ClampNonNegative(1f - hitContext.DamageReduction);
    }
}
