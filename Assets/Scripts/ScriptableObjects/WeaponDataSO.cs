using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器数据资源：
/// - 提供武器 prefab；
/// - 提供武器基础属性；
/// - 可选提供默认攻击序列；
/// - 提供该武器可使用的弹射物列表；
/// - 提供武器攻击表现层的基础配置；
/// - 作为 UI 展示层的描述来源。
/// 当前它只承担“数据来源”与“描述来源”职责，
/// 运行时攻击逻辑仍然在 Weapon / MeleeWeapon / RangeWeapon 中。
/// </summary>
[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ItemDataSO, IDescriptionSource
{
    [Header("Runtime")]
    [Tooltip("运行时实例化的武器 prefab。")]
    [SerializeField] protected Weapon weaponPrefab;
    [Tooltip("可选默认攻击序列。为空时武器运行时会自行生成代码级默认序列。")]
    [SerializeField] private AttackSequenceDefinitionSO attackSequence;

    [Header("Attack Presentation")]
    [Tooltip("是否在攻击准备完成后停止继续追踪瞄准。开启后，武器进入可攻击状态或攻击中时会锁定当前朝向，避免动作播放时还在持续转向导致动画变形。")]
    [SerializeField] private bool stopAimingWhenAttackReady = true;
    [Tooltip("一次攻击序列最多占用本次攻击间隔的百分比。1 代表动画可以刚好占满整个攻击间隔；0.8 代表会压缩到 80%，留 20% 给下次攻击前的准备/停顿。")]
    [Range(0.1f, 1f)]
    [SerializeField] private float attackSequenceOccupancy = 0.85f;

    [Header("Projectile List")]
    [Tooltip("该武器可使用的弹射物定义列表。序列里的 ProjectileVariantIndex 会映射到这里；近战武器也可以为命中后生成弹射物预留这些定义。")]
    [SerializeField] private ProjectileDefinitionSO[] projectileDefinitions;

    [Header("属性")]
    [Tooltip("攻击力：固定值，直接加到武器伤害。")]
    [SerializeField] protected float attack;
    [Tooltip("攻击速度：倍率，1 代表 100% 攻速。")]
    [SerializeField] protected float attackSpeed = 1f;
    [Tooltip("暴击率：概率，使用 0~1 表示，例如 0.05 = 5%。")]
    [SerializeField] protected float criticalChance;
    [Tooltip("武器暴击倍率：2 代表 200% 暴击伤害。")]
    [SerializeField] protected float criticalPercent = 2f;
    [Tooltip("攻击范围：固定值，直接增加武器索敌/攻击范围。")]
    [SerializeField] protected float range;

    public Weapon WeaponPrefab => weaponPrefab;
    public AttackSequenceDefinitionSO AttackSequence => attackSequence;
    public bool StopAimingWhenAttackReady => stopAimingWhenAttackReady;
    public float AttackSequenceOccupancy => Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
    public IReadOnlyList<ProjectileDefinitionSO> ProjectileDefinitions => projectileDefinitions;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalPercent = Mathf.Max(1f, criticalPercent);
        range = Mathf.Max(0f, range);
        attackSequenceOccupancy = Mathf.Clamp(attackSequenceOccupancy, 0.1f, 1f);
    }

    /// <summary>
    /// 返回武器基础属性条目。
    /// 等级修正不在这里做，而是交给 WeaponPropsCalculator 统一处理。
    /// </summary>
    public List<PropEntry> GetPropsList()
    {
        return new List<PropEntry>
        {
            new(PropType.Attack, attack),
            new(PropType.AttackSpeed, attackSpeed),
            new(PropType.CriticalChance, criticalChance),
            new(PropType.CriticalPercent, criticalPercent),
            new(PropType.Range, range)
        };
    }

    public IReadOnlyList<string> GetDescriptions()
    {
        return GetDescriptions(1);
    }

    /// <summary>
    /// 生成指定等级下用于 UI 展示的描述列表。
    /// </summary>
    public List<string> GetDescriptions(int level)
    {
        List<PropEntry> entries = GetPropEntriesByLevel(level);
        return FeatureDescriptionBuilder.BuildPropDescriptions(entries);
    }

    public List<PropEntry> GetPropEntriesByLevel(int level)
    {
        return WeaponPropsCalculator.GetPropEntries(this, level);
    }

    public Dictionary<PropType, float> GetPropsByLevel(int level)
    {
        Dictionary<PropType, float> dictionary = new();
        List<PropEntry> entries = GetPropEntriesByLevel(level);
        for (int i = 0; i < entries.Count; i++)
        {
            PropEntry entry = entries[i];
            dictionary[entry.propType] = entry.value;
        }

        return dictionary;
    }
}
