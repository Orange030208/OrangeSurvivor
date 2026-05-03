using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = ScriptableObjectMenuPaths.ACCESSORY, order = 0)]
public class AccessoryDataSO : ItemDataSO
{
    [SerializeField] protected string accessoryId;
    [SerializeField] protected int recyclePrice;

    [SerializeField] private AccessoryRarity rarity;

    [Header("属性修饰")]
    [Tooltip("按照属性语义填写。百分比与倍率统一使用百分比点：1 表示 1%。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => recyclePrice;
    public AccessoryRarity RarityGrade => rarity;
    public int Rarity => (int)rarity;
    public override string Description => BuildDescription();
    
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
        return ItemDescriptionUtility.BuildDescriptorInfos(
            itemDescription,
            propertyModifiers,
            specialFeatures,
            BuildMetaInfos());
    }

    private string BuildDescription()
    {
        return ItemDescriptionUtility.BuildDetailedDescription(
            itemDescription,
            propertyModifiers,
            specialFeatures,
            BuildMetaLines(),
            "提供一项饰品效果。");
    }

    private IEnumerable<DescriptorInfo> BuildMetaInfos()
    {
        yield return new DescriptorInfo("品质", ItemDescriptionUtility.FormatRarity(rarity));
    }

    private IEnumerable<ItemDescriptionLine> BuildMetaLines()
    {
        yield return new ItemDescriptionLine(
            "品质",
            ItemDescriptionUtility.FormatRarity(rarity),
            ItemDescriptionLineKind.Meta);
    }
}
