using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器属性计算器：
/// 根据武器基础数据和等级，生成当前等级下的属性条目与属性字典。
/// 当前实现是一个非常直接的线性倍率模型：
/// level 越高，所有属性统一乘上同一个倍率。
/// 这套逻辑足够简单易懂，但如果后续要做每级成长曲线、稀有度系数、分段成长，
/// 建议在这里集中升级，而不是把等级修正散落到各个武器运行时里。
/// </summary>
public static class WeaponPropsCalculator
{
    private const int MaxLevel = 6;

    /// <summary>
    /// 生成指定等级下的属性条目列表。
    /// 当前默认倍率公式：1 + level / MaxLevel。
    /// </summary>
    public static List<PropEntry> GetPropEntries(WeaponDataSO weaponData, int level)
    {
        float multiplier = 1f + (float)level / MaxLevel;

        List<PropEntry> calculatedProps = new();
        foreach (PropEntry propEntry in weaponData.GetPropsList())
        {
            calculatedProps.Add(new PropEntry(propEntry.propType, propEntry.modifierType, propEntry.value * multiplier));
        }

        return calculatedProps;
    }

    /// <summary>
    /// 生成指定等级下按 PropType 索引的属性字典。
    /// 适合运行时快速读取攻击、攻速、暴击等值。
    /// </summary>
    public static Dictionary<PropType, float> GetProps(WeaponDataSO weaponData, int level)
    {
        Dictionary<PropType, float> calculatedProps = new();
        List<PropEntry> entries = GetPropEntries(weaponData, level);
        for (int i = 0; i < entries.Count; i++)
        {
            PropEntry entry = entries[i];
            calculatedProps[entry.propType] = entry.value;
        }

        return calculatedProps;
    }
}
