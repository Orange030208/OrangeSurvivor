using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = ScriptableObjectMenuPaths.ACCESSORY, order = 0)]
public class AccessoryDataSO : ItemDataSO
{
    [SerializeField] protected string accessoryId;
    [SerializeField] protected int recyclePrice;

    [Range(0, 3)]
    [SerializeField] private int rarity;

    [Header("属性修饰")]
    [Tooltip("按照属性语义填写。倍率统一使用 0~1 表示 0%~100%。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => recyclePrice;
    public int Rarity => rarity;
    
    public IReadOnlyList<PropModifierData> PropertyModifiers => propertyModifiers;
    
    public IReadOnlyList<FeatureEffectBase> SpecialFeatures => specialFeatures;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(accessoryId))
        {
            accessoryId = Guid.NewGuid().ToString("N")[..8];
        }

        itemType = ItemType.Accessory;
    }

    public Dictionary<PropType, float> GetProps()
    {
        Dictionary<PropType, float> dictionary = new();
        foreach (PropModifierData modifier in propertyModifiers)
        {
            if (modifier.modifierType != PropModifierType.Add)
            {
                continue;
            }

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
        List<DescriptorInfo> infos = new();
        foreach (PropModifierData propEntry in propertyModifiers)
        {
            infos.Add(new DescriptorInfo(propEntry.GetDisplayName(),
                propEntry.propType.GetIconRichTextWithVOffset() + propEntry.GetDisplayName() + propEntry.GetDisplayValueText()));
        }

        foreach (FeatureEffectBase feature in specialFeatures)
        {
            infos.Add(new DescriptorInfo(feature.Title, feature.Description));
        }

        return infos;
    }
}
