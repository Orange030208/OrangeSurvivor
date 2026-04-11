using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ItemDataSO, IFeatureSource
{
    [SerializeField] protected Weapon weaponPrefab;

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
    public string FeatureSourceName => ItemName;
    public Sprite FeatureSourceIcon => ItemIcon;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalPercent = Mathf.Max(1f, criticalPercent);
        range = Mathf.Max(0f, range);
    }

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

    public List<string> GetAutoDescriptions(int level = 1)
    {
        List<PropEntry> entries = GetPropEntriesByLevel(level);
        return FeatureDescriptionBuilder.BuildPropDescriptions(entries);
    }

    public IReadOnlyList<PropEntry> GetFeaturePropEntries()
    {
        return GetPropsList();
    }

    public IReadOnlyList<IFeatureDefinition> GetSpecialFeatureDefinitions()
    {
        return System.Array.Empty<IFeatureDefinition>();
    }

    public IReadOnlyList<FeatureViewData> GetFeatureViewData()
    {
        List<PropEntry> props = GetPropsList();
        List<FeatureViewData> features = new(props.Count);
        FeatureDescriptionBuilder.AddPropFeatureViews(features, props);
        return features;
    }

    public IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId)
    {
        List<PropEntry> props = GetPropsList();
        List<FeatureEffectBase> effects = new(props.Count);

        for (int i = 0; i < props.Count; i++)
        {
            PropEntry modifier = props[i];
            string effectId = $"{runtimeSourceId}_{modifier.propType}_{modifier.modifierType}_{i}";
            effects.Add(new PropertyModifierEffect(effectId, effectId, modifier));
        }

        return effects;
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
