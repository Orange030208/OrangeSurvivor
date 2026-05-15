using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = ScriptableObjectMenuPaths.ACCESSORY, order = 0)]
public class AccessoryDataSO : ItemDataSO
{
    [SerializeField] protected string accessoryId;
    [SerializeField] protected int recyclePrice;

    [SerializeField] private AccessoryRarity rarity;

    [Header("持有规则")]
    [Tooltip("0 表示不限持有；1 表示唯一；大于 1 表示最多可同时持有的数量。")]
    [SerializeField, Min(0)] private int maxOwnedCount;

    [Header("属性修饰")]
    [Tooltip("按照属性语义填写。百分比属性与所有乘区统一使用百分比点：1 表示 1%，10 表示 10%。点数属性仍按属性单位填写。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference] private List<FeatureEffectBase> specialFeatures = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => recyclePrice;
    public AccessoryRarity RarityGrade => rarity;
    public int Rarity => (int)rarity;
    public int MaxOwnedCount => Mathf.Max(0, maxOwnedCount);
    public bool HasOwnedLimit => MaxOwnedCount > 0;
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
        maxOwnedCount = Mathf.Max(0, maxOwnedCount);
    }

    public bool CanOwnMore(int currentCount)
    {
        return !HasOwnedLimit || Mathf.Max(0, currentCount) < MaxOwnedCount;
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
            ItemDescriptionUtility.NormalizeManualDescription(itemDescription),
            propertyModifiers,
            specialFeatures,
            BuildMetaInfos());
    }

    private string BuildDescription()
    {
        return ItemDescriptionUtility.BuildDetailedDescription(
            ItemDescriptionUtility.NormalizeManualDescription(itemDescription),
            propertyModifiers,
            specialFeatures,
            BuildMetaLines(),
            string.Empty);
    }

    private IEnumerable<DescriptorInfo> BuildMetaInfos()
    {
        yield return new DescriptorInfo("品质", ItemDescriptionUtility.FormatRarity(rarity));
        if (HasOwnedLimit)
        {
            yield return new DescriptorInfo("持有上限", MaxOwnedCount.ToString());
        }
    }

    private IEnumerable<ItemDescriptionLine> BuildMetaLines()
    {
        yield return new ItemDescriptionLine(
            "品质",
            ItemDescriptionUtility.FormatRarity(rarity),
            ItemDescriptionLineKind.Meta);
        if (HasOwnedLimit)
        {
            yield return new ItemDescriptionLine(
                "持有上限",
                MaxOwnedCount.ToString(),
                ItemDescriptionLineKind.Meta);
        }
    }
}
