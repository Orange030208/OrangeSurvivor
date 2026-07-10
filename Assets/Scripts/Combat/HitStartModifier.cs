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

        if (target.TryGetComponent(out AttributeManager AttributeManager))
        {
            hitContext.DodgeChance = PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.Dodge,
                AttributeManager.GetAttributeValue(PropType.Dodge));
            float armorReduction = PropValueUtility.ResolveArmorDamageReductionRatio(
                AttributeManager.GetAttributeValue(PropType.Armor));
            float damageReduction = PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.DamageReduction,
                AttributeManager.GetAttributeValue(PropType.DamageReduction));
            hitContext.DamageReduction = PropValueUtility.CombineDamageReductionRatios(armorReduction, damageReduction);
            float knockbackResistance = PropValueUtility.PercentPointsToEffectiveRatio(
                PropType.KnockbackResistance,
                AttributeManager.GetAttributeValue(PropType.KnockbackResistance));
            hitContext.KnockbackStrength = PropValueUtility.ClampEffectiveKnockbackStrength(
                hitContext.KnockbackStrength * (1f - knockbackResistance));
        }

        hitContext.IsCritical = Random.value <= hitContext.CritChance;
        hitContext.IsDodged = Random.value <= hitContext.DodgeChance;
    }
}
