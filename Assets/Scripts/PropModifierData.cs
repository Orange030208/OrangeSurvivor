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
    public const float HEALTH_RECOVERY_POINTS_PER_HEALTH_PER_SECOND = 10f;
    public const float MAX_EFFECTIVE_CRITICAL_CHANCE_RATIO = 1f;
    public const float MAX_EFFECTIVE_DODGE_CHANCE_RATIO = 0.5f;
    public const float MAX_EFFECTIVE_DAMAGE_REDUCTION_RATIO = 0.5f;
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

    public static float HealthRecoveryPointsToHealthPerSecond(float value)
    {
        return value / HEALTH_RECOVERY_POINTS_PER_HEALTH_PER_SECOND;
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

    public static bool IsAdditivePercentMultiplierProp(PropType propType)
    {
        return propType == PropType.AttackSpeed;
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
