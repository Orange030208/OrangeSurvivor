using UnityEngine;

public class HitStartModifier : IHitModifier
{
    private const float ARMOR_REDUCTION_SCALE = 100f;
    private const float MIN_ARMOR = -95f;
    private const float MAX_SEQUENTIAL_REDUCTION = 0.95f;

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
            hitContext.DodgeChance = Mathf.Clamp01(PropValueUtility.PercentPointsToRatio(propertiesManager.GetPropValue(PropType.Dodge)));
            float armorReduction = ResolveArmorDamageReduction(
                propertiesManager.GetPropValue(PropType.Armor),
                hitContext.ArmorPenetrationPercent);
            float damageReduction = Mathf.Clamp01(PropValueUtility.PercentPointsToRatio(propertiesManager.GetPropValue(PropType.DamageReduction)));
            hitContext.DamageReduction = CombineSequentialReductions(armorReduction, damageReduction);
            float knockbackResistance = Mathf.Clamp01(PropValueUtility.PercentPointsToRatio(propertiesManager.GetPropValue(PropType.KnockbackResistance)));
            hitContext.KnockbackStrength = Mathf.Max(0f, hitContext.KnockbackStrength * (1f - knockbackResistance));
        }

        hitContext.IsCritical = Random.value <= hitContext.CritChance;
        hitContext.IsDodged = Random.value <= hitContext.DodgeChance;
    }

    private static float ResolveArmorDamageReduction(float armor, float armorPenetrationPercent)
    {
        armor = Mathf.Max(MIN_ARMOR, armor);
        if (armor > 0f && armorPenetrationPercent > 0f)
        {
            float armorPenetrationRatio = Mathf.Clamp01(PropValueUtility.PercentPointsToRatio(armorPenetrationPercent));
            armor *= 1f - armorPenetrationRatio;
        }

        return armor / (armor + ARMOR_REDUCTION_SCALE);
    }

    private static float CombineSequentialReductions(float firstReduction, float secondReduction)
    {
        firstReduction = Mathf.Min(firstReduction, MAX_SEQUENTIAL_REDUCTION);
        secondReduction = Mathf.Min(secondReduction, MAX_SEQUENTIAL_REDUCTION);
        return Mathf.Min(1f - (1f - firstReduction) * (1f - secondReduction), MAX_SEQUENTIAL_REDUCTION);
    }
}
