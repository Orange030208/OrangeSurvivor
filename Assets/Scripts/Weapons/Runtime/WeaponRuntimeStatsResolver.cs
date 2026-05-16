using System;

public readonly struct WeaponRuntimeStatsRequest
{
    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public PropertiesManager PropertiesManager { get; }
    public WeaponBenefitData WeaponBenefits { get; }

    public WeaponRuntimeStatsRequest(
        WeaponDataSO weaponData,
        int level,
        PropertiesManager propertiesManager,
        WeaponBenefitData weaponBenefits)
    {
        WeaponData = weaponData;
        Level = level;
        PropertiesManager = propertiesManager;
        WeaponBenefits = weaponBenefits;
    }
}

public readonly struct WeaponRuntimeStats
{
    public float Damage { get; }
    public float AttackInterval { get; }
    public float CriticalChance { get; }
    public float CriticalMultiplier { get; }
    public float Range { get; }
    public float KnockbackStrength { get; }

    public WeaponRuntimeStats(
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

public sealed class WeaponRuntimeStatsResolver
{
    public WeaponRuntimeStats Resolve(in WeaponRuntimeStatsRequest request)
    {
        if (request.WeaponData == null)
        {
            throw new ArgumentNullException(nameof(request.WeaponData));
        }

        if (request.PropertiesManager == null)
        {
            throw new ArgumentNullException(nameof(request.PropertiesManager));
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
            PropValueUtility.PercentPointsToRatio(request.PropertiesManager.GetPropValue(PropType.CriticalChance)));
        float playerCriticalBonus = benefits.ApplyToExternalValue(
            PropType.CriticalPercent,
            PropValueUtility.PercentPointsToRatio(request.PropertiesManager.GetPropValue(PropType.CriticalPercent)));

        float resolvedAttackSpeedPoints = request.PropertiesManager.GetPropValueWithAdditionalBase(
            PropType.AttackSpeed,
            weaponAttackSpeed);
        float finalAttackSpeedPoints = benefits.ApplyToResolvedStat(
            PropType.AttackSpeed,
            weaponAttackSpeed,
            resolvedAttackSpeedPoints);
        float typedAttackContribution = ResolveAttackTypeContribution(request.PropertiesManager, benefits);
        float damageMultiplier = 1f + PropValueUtility.PercentPointsToRatio(
            request.PropertiesManager.GetPropValue(PropType.Damage));
        float damage = PropValueUtility.ClampNonNegative((weaponAttack + typedAttackContribution) * damageMultiplier);
        float attackInterval = PropValueUtility.AttackSpeedPointsToAttackInterval(finalAttackSpeedPoints);
        float criticalChance = PropValueUtility.ClampEffectiveRatio(
            PropType.CriticalChance,
            weaponCriticalChance + playerCriticalChance);
        float criticalMultiplier = PropValueUtility.ClampEffectiveCriticalMultiplier(
            weaponCriticalMultiplier + playerCriticalBonus);
        float resolvedRangePoints = request.PropertiesManager.GetPropValueWithAdditionalBase(
            PropType.AttackRange,
            weaponRange);
        float rangePoints = benefits.ApplyToResolvedStat(PropType.AttackRange, weaponRange, resolvedRangePoints);
        float range = PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(rangePoints);
        float resolvedKnockbackStrength = request.PropertiesManager.GetPropValueWithAdditionalBase(
            PropType.KnockbackStrength,
            weaponKnockbackStrength);
        float knockbackStrength = PropValueUtility.ClampEffectiveKnockbackStrength(
            benefits.ApplyToResolvedStat(
                PropType.KnockbackStrength,
                weaponKnockbackStrength,
                resolvedKnockbackStrength));

        return new WeaponRuntimeStats(
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

    private static float ResolveAttackTypeContribution(PropertiesManager propertiesManager, WeaponBenefitData benefits)
    {
        if (!benefits.HasAnyUsage)
        {
            return 0f;
        }

        return ResolveAttackTypeContribution(propertiesManager, PropType.MeleeAttack, benefits.MeleeAttackUsagePercent) +
               ResolveAttackTypeContribution(propertiesManager, PropType.RangedAttack, benefits.RangedAttackUsagePercent) +
               ResolveAttackTypeContribution(propertiesManager, PropType.MagicAttack, benefits.MagicAttackUsagePercent) +
               ResolveAttackTypeContribution(propertiesManager, PropType.SummonAttack, benefits.SummonAttackUsagePercent);
    }

    private static float ResolveAttackTypeContribution(
        PropertiesManager propertiesManager,
        PropType propType,
        float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            return 0f;
        }

        return propertiesManager.GetPropValue(propType) * PropValueUtility.PercentPointsToRatio(usagePercent);
    }
}
