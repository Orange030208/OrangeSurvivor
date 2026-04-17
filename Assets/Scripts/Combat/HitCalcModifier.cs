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

        if (hitContext.IsCancelled || hitContext.IsDodged)
        {
            hitContext.Damage = 0f;
            return;
        }

        float damage = Mathf.Max(0f, hitContext.Damage);
        if (hitContext.IsCritical)
        {
            damage *= hitContext.CritMultiplier;
        }

        hitContext.Damage = damage * Mathf.Max(0f, 1f - hitContext.DamageReduction);
    }
}
