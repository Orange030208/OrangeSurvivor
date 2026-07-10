using System;
using UnityEngine;

public readonly struct WeaponStatsRequest
{
    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public AttributeManager AttributeManager { get; }
    public WeaponBenefitData WeaponBenefits { get; }

    public WeaponStatsRequest(
        WeaponDataSO weaponData,
        int level,
        AttributeManager attributeManager,
        WeaponBenefitData weaponBenefits)
    {
        WeaponData = weaponData;
        Level = level;
        AttributeManager = attributeManager;
        WeaponBenefits = weaponBenefits;
    }
}

public readonly struct WeaponStats
{
    public float Damage { get; }
    public float AttackInterval { get; }
    public float CriticalChance { get; }
    public float CriticalMultiplier { get; }
    public float Range { get; }
    public float KnockbackStrength { get; }

    public WeaponStats(
        float damage,
        float attackInterval,
        float criticalChance,
        float criticalMultiplier,
        float range,
        float knockbackStrength)
    {
        Damage = damage;
        AttackInterval = attackInterval;
        CriticalChance = criticalChance;
        CriticalMultiplier = criticalMultiplier;
        Range = range;
        KnockbackStrength = knockbackStrength;
    }
}

public sealed class WeaponStatsResolver
{
    public WeaponStats Resolve(in WeaponStatsRequest request)
    {
        if (request.WeaponData == null)
        {
            throw new ArgumentNullException(nameof(request.WeaponData));
        }

        if (request.AttributeManager == null)
        {
            throw new ArgumentNullException(nameof(request.AttributeManager));
        }

        WeaponLevelStatData weaponStats = request.WeaponData.GetLevelStats(request.Level);

        float weaponAttack = weaponStats.Attack;
        float weaponAttackSpeed = weaponStats.AttackSpeed;
        float weaponCriticalChance = PropValueUtility.PercentPointsToRatio(weaponStats.CriticalChance);
        float weaponCriticalMultiplier = PropValueUtility.PercentPointsToRatio(weaponStats.CriticalPercent);
        float weaponRange = weaponStats.Range;
        float weaponKnockbackStrength = weaponStats.KnockbackStrength;
        WeaponBenefitData benefits = ResolveWeaponBenefits(weaponStats, request.WeaponBenefits);

        float playerCriticalChance = benefits.ApplyToExternalValue(
            PropType.CriticalChance,
            PropValueUtility.PercentPointsToRatio(request.AttributeManager.GetAttributeValue(PropType.CriticalChance)));
        float playerCriticalBonus = benefits.ApplyToExternalValue(
            PropType.CriticalPercent,
            PropValueUtility.PercentPointsToRatio(request.AttributeManager.GetAttributeValue(PropType.CriticalPercent)));

        float resolvedAttackSpeedPoints = request.AttributeManager.GetAttributeValueWithAdditionalBase(
            PropType.AttackSpeed,
            Mathf.RoundToInt(weaponAttackSpeed));
        float finalAttackSpeedPoints = benefits.ApplyToResolvedStat(
            PropType.AttackSpeed,
            weaponAttackSpeed,
            resolvedAttackSpeedPoints);
        float typedAttackContribution = ResolveAttackTypeContribution(request.AttributeManager, benefits);
        float damageMultiplier = 1f + PropValueUtility.PercentPointsToRatio(
            request.AttributeManager.GetAttributeValue(PropType.Damage));
        float damage = PropValueUtility.ClampNonNegative((weaponAttack + typedAttackContribution) * damageMultiplier);
        float attackInterval = PropValueUtility.AttackSpeedPointsToAttackInterval(finalAttackSpeedPoints);
        float criticalChance = PropValueUtility.ClampEffectiveRatio(
            PropType.CriticalChance,
            weaponCriticalChance + playerCriticalChance);
        float criticalMultiplier = PropValueUtility.ClampEffectiveCriticalMultiplier(
            weaponCriticalMultiplier + playerCriticalBonus);
        float resolvedRangePoints = request.AttributeManager.GetAttributeValueWithAdditionalBase(
            PropType.AttackRange,
            Mathf.RoundToInt(weaponRange));
        float rangePoints = benefits.ApplyToResolvedStat(PropType.AttackRange, weaponRange, resolvedRangePoints);
        float range = PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(rangePoints);
        float resolvedKnockbackStrength = request.AttributeManager.GetAttributeValueWithAdditionalBase(
            PropType.KnockbackStrength,
            Mathf.RoundToInt(weaponKnockbackStrength));
        float knockbackStrength = PropValueUtility.ClampEffectiveKnockbackStrength(
            benefits.ApplyToResolvedStat(
                PropType.KnockbackStrength,
                weaponKnockbackStrength,
                resolvedKnockbackStrength));

        return new WeaponStats(
            damage,
            attackInterval,
            criticalChance,
            criticalMultiplier,
            range,
            knockbackStrength);
    }

    private static WeaponBenefitData ResolveWeaponBenefits(
        WeaponLevelStatData weaponStats,
        WeaponBenefitData weaponBenefits)
    {
        return weaponBenefits + weaponStats.StatBenefits;
    }

    private static float ResolveAttackTypeContribution(AttributeManager attributeManager, WeaponBenefitData benefits)
    {
        if (!benefits.HasAnyUsage)
        {
            return 0f;
        }

        return ResolveAttackTypeContribution(attributeManager, PropType.MeleeAttack, benefits.MeleeAttackUsagePercent) +
               ResolveAttackTypeContribution(attributeManager, PropType.RangedAttack, benefits.RangedAttackUsagePercent) +
               ResolveAttackTypeContribution(attributeManager, PropType.MagicAttack, benefits.MagicAttackUsagePercent) +
               ResolveAttackTypeContribution(attributeManager, PropType.SummonAttack, benefits.SummonAttackUsagePercent);
    }

    private static float ResolveAttackTypeContribution(
        AttributeManager attributeManager,
        PropType propType,
        float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            return 0f;
        }

        return attributeManager.GetAttributeValue(propType) * PropValueUtility.PercentPointsToRatio(usagePercent);
    }
}
