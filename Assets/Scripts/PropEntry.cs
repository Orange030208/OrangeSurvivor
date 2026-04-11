using System;
using UnityEngine;

public enum PropModifierType
{
    Flat,
    BasePercent,
    FinalFlat,
    FinalPercent
}

/// <summary>
/// 单条属性修饰配置。
/// 用于角色额外属性、饰品属性修饰，以及运行时属性修饰效果的统一载体。
/// </summary>
[Serializable]
public struct PropEntry
{
    /// <summary>
    /// 被修饰的属性类型。
    /// </summary>
    public PropType propType;

    /// <summary>
    /// 修饰的结算方式。
    /// </summary>
    public PropModifierType modifierType;

    /// <summary>
    /// 修饰值本身。
    /// 含义由 propType 与 modifierType 共同决定。
    /// </summary>
    public float value;

    public PropEntry(PropType propType, float value)
    {
        this.propType = propType;
        modifierType = PropModifierType.Flat;
        this.value = value;
    }

    public PropEntry(PropType propType, PropModifierType modifierType, float value)
    {
        this.propType = propType;
        this.modifierType = modifierType;
        this.value = value;
    }

    public readonly string GetDisplayName()
    {
        return modifierType switch
        {
            PropModifierType.Flat => propType.GetChineseName(),
            PropModifierType.BasePercent => $"{propType.GetChineseName()}（基础%）",
            PropModifierType.FinalFlat => $"{propType.GetChineseName()}（最终+）",
            PropModifierType.FinalPercent => $"{propType.GetChineseName()}（最终%）",
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
