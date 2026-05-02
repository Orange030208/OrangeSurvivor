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

    public static float PercentPointsToRatio(float value)
    {
        return value * PERCENT_POINT_TO_RATIO;
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
               propType == PropType.HealingPower;
    }

    public static bool IsAdditivePercentMultiplierProp(PropType propType)
    {
        return propType == PropType.AttackSpeed;
    }
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
        string propName = ResourcesManager.GetPropDisplayName(propType);
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
