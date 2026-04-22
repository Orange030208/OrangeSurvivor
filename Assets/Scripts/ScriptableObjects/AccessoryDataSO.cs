using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = "SO/Accessory", order = 0)]
public class AccessoryDataSO : ItemDataSO, IRuntimeFeatureSource
{
    [SerializeField] protected string accessoryId;
    [SerializeField] protected int recyclePrice;

    [Range(0, 3)]
    [SerializeField]
    private int rarity;

    [Header("属性修饰")]
    [Tooltip("按照 PropType 的语义填写：概率/比例统一使用 0~1，倍率类通常使用 1 代表 100%。")]
    [SerializeField] private List<PropEntry> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => recyclePrice;
    public int Rarity => rarity;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(accessoryId))
        {
            accessoryId = Guid.NewGuid().ToString("N")[..8];
        }
        itemType = ItemType.Accessory;
    }

    public IReadOnlyList<PropEntry> GetPropEntries()
    {
        return propertyModifiers;
    }

    public IReadOnlyList<FeatureEffectBase> CreateRuntimeFeatureEffects(string runtimeSourceId)
    {
        List<FeatureEffectBase> effects = new(propertyModifiers.Count + specialFeatures.Count);

        for (int i = 0; i < propertyModifiers.Count; i++)
        {
            PropEntry modifier = propertyModifiers[i];
            string effectId = $"{runtimeSourceId}_{modifier.propType}_{modifier.modifierType}_{i}";
            effects.Add(new PropertyModifierEffect(effectId, effectId, modifier));
        }

        for (int i = 0; i < specialFeatures.Count; i++)
        {
            FeatureEffectBase feature = specialFeatures[i];
            if (feature == null)
            {
                continue;
            }

            feature.RuntimeFeatureId = $"{runtimeSourceId}_FEATURE_{i}";
            effects.Add(feature);
        }

        return effects;
    }

    public Dictionary<PropType, float> GetProps()
    {
        var dictionary = new Dictionary<PropType, float>();
        foreach (var modifier in propertyModifiers)
        {
            if (dictionary.TryGetValue(modifier.propType, out float currentValue))
            {
                dictionary[modifier.propType] = currentValue + modifier.value;
            }
            else
            {
                dictionary[modifier.propType] = modifier.value;
            }
        }

        return dictionary;
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new List<DescriptorInfo>();
        foreach (PropEntry propEntry in propertyModifiers)
        {
            infos.Add(new DescriptorInfo(propEntry.GetDisplayName(),
                propEntry.propType.GetIconRichTextWithVOffset() + propEntry.GetDisplayName() + propEntry.value));
        }
        
        foreach (var feature in specialFeatures)
        {
            infos.Add(new DescriptorInfo(feature.Title, feature.Description));
        }
        
        return infos;
    }
}
