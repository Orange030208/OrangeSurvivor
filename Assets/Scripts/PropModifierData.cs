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
        return modifierType switch
        {
            PropModifierType.Add => propType.GetChineseName(),
            PropModifierType.BaseMultiplier => $"{propType.GetChineseName()}（基础乘区）",
            PropModifierType.BonusMultiplier => $"{propType.GetChineseName()}（加成乘区）",
            PropModifierType.FinalMultiplier => $"{propType.GetChineseName()}（最终乘区）",
            _ => propType.GetChineseName()
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
