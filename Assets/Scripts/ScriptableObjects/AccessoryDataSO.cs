using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = ScriptableObjectMenuPaths.ACCESSORY, order = 0)]
public class AccessoryDataSO : ItemDataSO, IInfoDocumentSource, IHasContentTier
{
    private const float RECYCLE_PRICE_MULTIPLIER = 1f / 3f;

    [SerializeField] protected string accessoryId;

    [FormerlySerializedAs("rarity")]
    [SerializeField] private ContentTier tier;

    [Header("持有规则")]
    [Tooltip("0 表示不限持有；1 表示唯一；大于 1 表示最多可同时持有的数量。")]
    [SerializeField, Min(0)] private int maxOwnedCount;

    [Header("属性修饰")]
    [Tooltip("按照属性语义填写。百分比属性与所有乘区统一使用百分比点：1 表示 1%，10 表示 10%。点数属性仍按属性单位填写。")]
    [SerializeField] private List<PropModifierData> propertyModifiers = new();

    [Header("特殊能力")]
    [SerializeReference, TypeFilter("@FeatureTypeSelectionUtility.GetSelectableFeatureTypes()")]
    [ListDrawerSettings(Expanded = true)]
    private List<FeatureBase> specialFeatures = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => PropValueUtility.ResolveNonNegativePrice(ItemPrice * RECYCLE_PRICE_MULTIPLIER);
    public ContentTier Tier => tier;
    public int MaxOwnedCount => Mathf.Max(0, maxOwnedCount);
    public bool HasOwnedLimit => MaxOwnedCount > 0;
    public override string Description => BuildDescription();
    
    public IReadOnlyList<PropModifierData> PropertyModifiers => propertyModifiers;
    
    public IReadOnlyList<FeatureBase> SpecialFeatures => specialFeatures;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(accessoryId))
        {
            accessoryId = Guid.NewGuid().ToString("N")[..8];
        }

        itemType = ItemType.Accessory;
        tier = ContentTierResolver.FromQualityValue((int)tier);
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

    public InfoDocument BuildInfoDocument()
    {
        return new AccessoryInfoBuilder().Build(this);
    }

    private string BuildDescription()
    {
        InfoDocument document = BuildInfoDocument();
        return InfoDocumentTextFormatter.ToPlainText(document, includeHeader: false);
    }
}
