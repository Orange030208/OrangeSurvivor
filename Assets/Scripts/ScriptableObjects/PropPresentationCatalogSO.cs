using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Prop Presentation Catalog", menuName = ScriptableObjectMenuPaths.PROP_PRESENTATION_CATALOG, order = 0)]
public sealed class PropPresentationCatalogSO : ScriptableObject
{
    [Tooltip("属性展示配置列表。每种属性类型应只保留一条，用于把描述数据里的属性名映射为界面可用的中文名与图标。")]
    [SerializeField] private List<PropPresentationEntry> entries = new List<PropPresentationEntry>();

    public IReadOnlyList<PropPresentationEntry> Entries => entries;

    /// <summary>
    /// 根据外部描述数据传入的属性名查找展示配置。
    /// 目前支持中文名与 PropType 枚举名，便于调用方逐步从旧文本数据迁移到结构化数据。
    /// </summary>
    public bool TryGetEntry(string propName, out PropPresentationEntry entry)
    {
        if (string.IsNullOrWhiteSpace(propName))
        {
            entry = default;
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PropPresentationEntry current = entries[i];
            if (current.Matches(propName))
            {
                entry = current;
                return true;
            }
        }

        entry = default;
        return false;
    }

    private void OnValidate()
    {
        // 保持资产总是包含完整属性集合，减少后续新增 PropType 后 UI 漏配的风险。
        EnsureAllPropEntries();
        entries.Sort(CompareEntryOrder);
    }

    [ContextMenu("Fill Missing Prop Entries")]
    private void EnsureAllPropEntries()
    {
        Array propTypes = Enum.GetValues(typeof(PropType));
        for (int i = 0; i < propTypes.Length; i++)
        {
            PropType propType = (PropType)propTypes.GetValue(i);
            int existingIndex = FindIndex(propType);
            if (existingIndex >= 0)
            {
                // 只补缺省中文名，不覆盖已经手动指定的中文名或图标。
                entries[existingIndex] = entries[existingIndex].WithDefaults();
                continue;
            }

            entries.Add(PropPresentationEntry.CreateDefault(propType));
        }
    }

    private int FindIndex(PropType propType)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].PropType == propType)
            {
                return i;
            }
        }

        return -1;
    }

    private static int CompareEntryOrder(PropPresentationEntry left, PropPresentationEntry right)
    {
        return ((int)left.PropType).CompareTo((int)right.PropType);
    }
}

[Serializable]
public struct PropPresentationEntry
{
    [Tooltip("属性类型。用于稳定排序，也作为英文属性名匹配来源。")]
    [SerializeField] private PropType propType;
    [Tooltip("界面上展示的属性中文名，也用于匹配描述信息的标签。")]
    [SerializeField] private string chineseName;
    [Tooltip("属性说明文本，用于悬浮提示、详情面板等需要解释属性含义的界面。")]
    [SerializeField] private string description;
    [Tooltip("界面上展示的属性图标。默认创建时暂时留空，可在检视面板中手动配置。")]
    [SerializeField] private Sprite icon;

    public PropType PropType => propType;
    public string ChineseName => chineseName;
    public string Description => description;
    public Sprite Icon => icon;

    public PropPresentationEntry(PropType propType, string chineseName, string description, Sprite icon)
    {
        this.propType = propType;
        this.chineseName = chineseName;
        this.description = description;
        this.icon = icon;
    }

    public static PropPresentationEntry CreateDefault(PropType propType)
    {
        return new PropPresentationEntry(propType, GetDefaultChineseName(propType), string.Empty, null);
    }

    public bool Matches(string propName)
    {
        return string.Equals(propName, chineseName, StringComparison.Ordinal) ||
               string.Equals(propName, propType.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 生成一个补齐缺省展示数据的新条目，避免 OnValidate 直接修改结构体字段时产生隐式副本问题。
    /// </summary>
    public PropPresentationEntry WithDefaults()
    {
        string resolvedChineseName = string.IsNullOrWhiteSpace(chineseName) ? GetDefaultChineseName(propType) : chineseName;
        string resolvedDescription = description ?? string.Empty;
        return new PropPresentationEntry(propType, resolvedChineseName, resolvedDescription, icon);
    }

    private static string GetDefaultChineseName(PropType propType)
    {
        return propType switch
        {
            PropType.Attack => "攻击力",
            PropType.AttackSpeed => "攻击速度",
            PropType.CriticalChance => "暴击率",
            PropType.CriticalPercent => "暴击伤害",
            PropType.MoveSpeed => "移动速度",
            PropType.MaxHealth => "最大生命值",
            PropType.AttackRange => "攻击范围",
            PropType.HealthRecoverySpeed => "生命恢复速度",
            PropType.Armor => "护甲",
            PropType.Luck => "幸运",
            PropType.Dodge => "闪避",
            PropType.LifeSteal => "生命偷取",
            PropType.PickupRadius => "拾取半径",
            PropType.ProjectileSpeed => "弹体速度",
            PropType.ProjectilePierceCount => "弹射物穿透数量",
            PropType.WeaponSlotCount => "武器槽位数量",
            PropType.KnockbackStrength => "击退强度",
            PropType.KnockbackResistance => "击退抗性",
            PropType.ExperienceGain => "经验获取",
            PropType.ShopPriceDiscount => "商店折扣",
            PropType.WaveGoldRewardBonus => "波次金币奖励",
            PropType.ShopFreeRerollCount => "商店免费刷新",
            PropType.ShopOfferCount => "商店商品数量",
            PropType.DamageReduction => "伤害减免",
            PropType.HealingPower => "治疗效果",
            PropType.Damage => "伤害提升",
            PropType.MeleeAttack => "近战攻击",
            PropType.RangedAttack => "远程攻击",
            PropType.MagicAttack => "魔法攻击",
            PropType.SummonAttack => "召唤攻击",
            _ => propType.ToString()
        };
    }
}
