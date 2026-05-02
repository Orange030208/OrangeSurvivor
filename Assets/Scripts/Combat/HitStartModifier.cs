using UnityEngine;

public class HitStartModifier : IHitModifier
{
    public int HitPriority => HitModifierPriority.Core;
    public HitModifierTiming HitModifierTiming => HitModifierTiming.Deal;

    public void ModifyHit(HitContext hitContext)
    {
        Entity target = hitContext.Request.Target;
        if (target == null)
        {
            hitContext.IsCancelled = true;
            return;
        }

        if (!target.TryGetComponent(out HealthComponent healthComponent))
        {
            hitContext.IsCancelled = true;
            return;
        }

        if (healthComponent.CurrentHealth <= 0f)
        {
            hitContext.IsCancelled = true;
            return;
        }

        if (target.TryGetComponent(out PropertiesManager propertiesManager))
        {
            hitContext.DodgeChance = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.Dodge));
            float armor = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.Armor));
            float damageReduction = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.DamageReduction));
            hitContext.DamageReduction = Mathf.Clamp01(armor + damageReduction);
            float knockbackResistance = Mathf.Clamp01(propertiesManager.GetPropValue(PropType.KnockbackResistance));
            hitContext.KnockbackStrength = Mathf.Max(0f, hitContext.KnockbackStrength * (1f - knockbackResistance));
        }

        hitContext.IsCritical = Random.value <= hitContext.CritChance;
        hitContext.IsDodged = Random.value <= hitContext.DodgeChance;
    }
}
