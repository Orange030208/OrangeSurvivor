using System;
using UnityEngine;

/// <summary>
/// 基础属性数据。
/// 仅描述某个属性的原始值。
/// </summary>
[Serializable]
public struct BasePropData
{
    public PropType propType;
    public float value;

    public BasePropData(PropType propType, float value)
    {
        this.propType = propType;
        this.value = value;
    }
}

public enum PropModifierType
{
    Add,
    BaseMultiplier,
    BonusMultiplier,
    FinalMultiplier
}

public static class PropValueUtility
{
    public const float PERCENT_POINT_TO_RATIO = 0.01f;
    public const float DISTANCE_POINTS_PER_WORLD_UNIT = 100f;
    public const float ATTACK_SPEED_POINTS_PER_ATTACK_PER_SECOND = 100f;
    public const float MIN_EFFECTIVE_ATTACK_SPEED_POINTS = 1f;
    public const float MIN_ATTACK_SPEED_BENEFIT_RATIO = 0.01f;
    public const float MIN_EFFECTIVE_ATTACK_RANGE_WORLD_UNITS = 0.1f;
    public const float MIN_EFFECTIVE_CRITICAL_MULTIPLIER = 1f;
    public const float HEALTH_RECOVERY_POINTS_PER_HEALTH_PER_SECOND = 10f;
    public const float MIN_EFFECTIVE_MAX_HEALTH = 1f;
    public const float ARMOR_REDUCTION_SCALE = 25f;
    public const float MIN_EFFECTIVE_ARMOR = -95f;
    public const float MAX_EFFECTIVE_CRITICAL_CHANCE_RATIO = 1f;
    public const float MAX_EFFECTIVE_DODGE_CHANCE_RATIO = 0.5f;
    public const float MAX_EFFECTIVE_DAMAGE_REDUCTION_RATIO = 0.5f;
    public const float MAX_EFFECTIVE_TOTAL_DAMAGE_REDUCTION_RATIO = 0.95f;
    public const float MAX_EFFECTIVE_SHOP_PRICE_DISCOUNT_RATIO = 0.5f;
    public const float MIN_EFFECTIVE_SHOP_PRICE_MULTIPLIER = 0.5f;

    public static float PercentPointsToRatio(float value)
    {
        return value * PERCENT_POINT_TO_RATIO;
    }

    public static float DistancePointsToWorldUnits(float value)
    {
        return value / DISTANCE_POINTS_PER_WORLD_UNIT;
    }

    public static float DistancePointsToNonNegativeWorldUnits(float value)
    {
        return Mathf.Max(0f, DistancePointsToWorldUnits(value));
    }

    public static float DistancePointsToEffectiveAttackRangeWorldUnits(float value)
    {
        return Mathf.Max(MIN_EFFECTIVE_ATTACK_RANGE_WORLD_UNITS, DistancePointsToWorldUnits(value));
    }

    public static float AttackSpeedPointsToAttacksPerSecond(float value)
    {
        return ClampEffectiveAttackSpeedPoints(value) / ATTACK_SPEED_POINTS_PER_ATTACK_PER_SECOND;
    }

    public static float ClampEffectiveAttackSpeedPoints(float value)
    {
        return Mathf.Max(MIN_EFFECTIVE_ATTACK_SPEED_POINTS, value);
    }

    public static float ClampAttackSpeedBenefitRatio(float value)
    {
        return Mathf.Max(MIN_ATTACK_SPEED_BENEFIT_RATIO, value);
    }

    public static float AttackSpeedPointsToAttackInterval(float value)
    {
        return 1f / AttackSpeedPointsToAttacksPerSecond(value);
    }

    public static float HealthRecoveryPointsToHealthPerSecond(float value)
    {
        return value / HEALTH_RECOVERY_POINTS_PER_HEALTH_PER_SECOND;
    }

    public static float ClampEffectiveMaxHealth(float value)
    {
        return Mathf.Max(MIN_EFFECTIVE_MAX_HEALTH, value);
    }

    public static float PercentPointsToNonNegativeRatio(float value)
    {
        return Mathf.Max(0f, PercentPointsToRatio(value));
    }

    public static float HealthRecoveryPointsToEffectiveHealthPerSecond(float value)
    {
        return Mathf.Max(0f, HealthRecoveryPointsToHealthPerSecond(value));
    }

    public static float ResolveArmorDamageReductionRatio(float armor, float armorPenetrationPercent)
    {
        float effectiveArmor = Mathf.Max(MIN_EFFECTIVE_ARMOR, armor);
        if (effectiveArmor > 0f && armorPenetrationPercent > 0f)
        {
            float armorPenetrationRatio = Mathf.Clamp01(PercentPointsToRatio(armorPenetrationPercent));
            effectiveArmor *= 1f - armorPenetrationRatio;
        }

        return effectiveArmor / (Mathf.Abs(effectiveArmor) + ARMOR_REDUCTION_SCALE);
    }

    public static float CombineDamageReductionRatios(float firstReduction, float secondReduction)
    {
        float clampedFirstReduction = Mathf.Min(firstReduction, MAX_EFFECTIVE_TOTAL_DAMAGE_REDUCTION_RATIO);
        float clampedSecondReduction = Mathf.Min(secondReduction, MAX_EFFECTIVE_TOTAL_DAMAGE_REDUCTION_RATIO);
        return Mathf.Min(
            1f - (1f - clampedFirstReduction) * (1f - clampedSecondReduction),
            MAX_EFFECTIVE_TOTAL_DAMAGE_REDUCTION_RATIO);
    }

    public static float ClampEffectiveKnockbackStrength(float value)
    {
        return Mathf.Max(0f, value);
    }

    public static float ClampEffectiveCriticalMultiplier(float value)
    {
        return Mathf.Max(MIN_EFFECTIVE_CRITICAL_MULTIPLIER, value);
    }

    public static float ClampNonNegative(float value)
    {
        return Mathf.Max(0f, value);
    }

    public static int FloatPointsToNonNegativeRoundedInt(float value)
    {
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public static int FloatPointsToNonNegativeFlooredInt(float value)
    {
        return Mathf.Max(0, Mathf.FloorToInt(value));
    }

    public static int ResolveNonNegativePrice(float value)
    {
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public static float ClampEffectiveRatio(PropType propType, float ratio)
    {
        float maxRatio = propType switch
        {
            PropType.CriticalChance => MAX_EFFECTIVE_CRITICAL_CHANCE_RATIO,
            PropType.Dodge => MAX_EFFECTIVE_DODGE_CHANCE_RATIO,
            PropType.DamageReduction => MAX_EFFECTIVE_DAMAGE_REDUCTION_RATIO,
            PropType.ShopPriceDiscount => MAX_EFFECTIVE_SHOP_PRICE_DISCOUNT_RATIO,
            PropType.KnockbackResistance => 1f,
            _ => 1f
        };

        return Mathf.Clamp(ratio, 0f, maxRatio);
    }

    public static float PercentPointsToEffectiveRatio(PropType propType, float value)
    {
        return ClampEffectiveRatio(propType, PercentPointsToRatio(value));
    }

    public static float ResolveEffectiveShopPriceMultiplier(float playerDiscountMultiplier)
    {
        return playerDiscountMultiplier > 0f
            ? Mathf.Max(MIN_EFFECTIVE_SHOP_PRICE_MULTIPLIER, playerDiscountMultiplier)
            : 1f;
    }

    public static bool IsPercentPointProp(PropType propType)
    {
        return propType == PropType.CriticalChance ||
               propType == PropType.CriticalPercent ||
               propType == PropType.Dodge ||
               propType == PropType.LifeSteal ||
               propType == PropType.ExperienceGain ||
               propType == PropType.ShopPriceDiscount ||
               propType == PropType.KnockbackResistance ||
               propType == PropType.DamageReduction ||
               propType == PropType.HealingPower ||
               propType == PropType.Damage;
    }

}

/// <summary>
/// 属性映射规则。
/// sourcePropType 的未含映射最终值会按 conversionPercent 转换为 targetPropType 的额外 Add。
/// </summary>
[Serializable]
public struct PropMappingData
{
    public PropType sourcePropType;
    public PropType targetPropType;
    [Tooltip("映射比例，百分比点口径：100 表示源属性 1 点映射为目标属性 +1，50 表示 +0.5。")]
    public float conversionPercent;

    public PropMappingData(PropType sourcePropType, PropType targetPropType, float conversionPercent)
    {
        this.sourcePropType = sourcePropType;
        this.targetPropType = targetPropType;
        this.conversionPercent = conversionPercent;
    }

    public readonly float ConversionRatio => PropValueUtility.PercentPointsToRatio(conversionPercent);
}

/// <summary>
/// 加成属性数据。
/// 用于描述某个属性的额外修饰值及其结算类型。
/// </summary>
[Serializable]
public struct PropModifierData
{
    public PropType propType;
    public PropModifierType modifierType;
    public float value;

    public PropModifierData(PropType propType, float value)
    {
        this.propType = propType;
        modifierType = PropModifierType.Add;
        this.value = value;
    }

    public PropModifierData(PropType propType, PropModifierType modifierType, float value)
    {
        this.propType = propType;
        this.modifierType = modifierType;
        this.value = value;
    }

    public readonly string GetDisplayName()
    {
        string propName = GameContentRuntime.GetPropDisplayName(propType);
        return modifierType switch
        {
            PropModifierType.Add => propName,
            PropModifierType.BaseMultiplier => $"{propName}（基础乘区）",
            PropModifierType.BonusMultiplier => $"{propName}（加成乘区）",
            PropModifierType.FinalMultiplier => $"{propName}（最终乘区）",
            _ => propName
        };
    }

    public readonly string GetDisplayValueText()
    {
        return propType.FormatModifierValue(modifierType, value);
    }

    public readonly string GetAutoDescription()
    {
        return propType.BuildModifierDescription(modifierType, value);
    }
}
