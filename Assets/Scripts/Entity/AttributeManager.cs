using System;
using System.Collections.Generic;
using Orange.Attributes;
using UnityEngine;

public class AttributeManager : EntityComponentBase
{
    private readonly AttributeSystem<PropType> attributeSystem = new();

    private Entity owner;

    [Header("属性映射")]
    [Tooltip("将一个属性的未映射最终值按比例转换为另一个属性的额外加值。当前不做递归映射。")]
    [SerializeField] private List<PropMappingData> attributeMappings = new();

    public event Action OnAttributesChanged;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        InitializeAttributes();
        NotifyAttributesChanged();
    }

    public void AddModifier(string sourceId, PropModifierData modifier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[AttributeManager] 添加属性修饰失败：来源 ID 为空。");
            return;
        }

        AddModifiers(sourceId, new List<PropModifierData> { modifier });
    }

    public void AddModifiers(string sourceId, IReadOnlyList<PropModifierData> modifiers)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[AttributeManager] 添加属性修饰失败：来源 ID 为空。");
            return;
        }

        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        List<AttributeModifier<PropType>> attributeModifiers = new(modifiers.Count);
        for (int i = 0; i < modifiers.Count; i++)
        {
            attributeModifiers.Add(ToAttributeModifier(modifiers[i]));
        }

        attributeSystem.AddModifiers(sourceId, attributeModifiers);
        RecalculateAllAttributes(notifyAllWhenUnchanged: true);
    }

    public void RemoveModifier(string sourceId, PropType propType, PropModifierType modifierType)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!attributeSystem.RemoveModifier(sourceId, propType, ToAttributeModifierType(modifierType)))
        {
            return;
        }

        RecalculateAllAttributes(notifyAllWhenUnchanged: true);
    }

    public void RemoveModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || !attributeSystem.RemoveModifiers(sourceId))
        {
            return;
        }

        RecalculateAllAttributes(notifyAllWhenUnchanged: true);
    }

    public int GetAttributeValue(PropType propType)
    {
        return attributeSystem.GetValue(propType);
    }

    public int GetAttributeValueWithAdditionalBase(PropType propType, int additionalBaseValue)
    {
        return attributeSystem.GetValueWithAdditionalBase(propType, additionalBaseValue);
    }

    public int GetBaseAttributeValue(PropType propType)
    {
        return attributeSystem.GetBaseValue(propType);
    }

    public Dictionary<PropType, int> GetAllAttributeValues()
    {
        Dictionary<PropType, int> attributeValues = attributeSystem.GetAllValues();
        Dictionary<PropType, int> result = new(attributeValues.Count);
        foreach (KeyValuePair<PropType, int> attributeValue in attributeValues)
        {
            result[attributeValue.Key] = attributeValue.Value;
        }

        return result;
    }

    public void SubscribeAttributeChanged(PropType propType, Action<int> handler)
    {
        if (handler == null)
        {
            Debug.LogWarning("[AttributeManager] 订阅属性变化失败：回调为空。");
            return;
        }

        attributeSystem.SubscribeValueChanged(propType, handler);
    }

    public void UnsubscribeAttributeChanged(PropType propType, Action<int> handler)
    {
        attributeSystem.UnsubscribeValueChanged(propType, handler);
    }

    public static int GetDefaultAttributeValue(PropType propType)
    {
        return propType switch
        {
            PropType.CriticalPercent => 0,
            PropType.ProjectileSpeed => 1,
            PropType.ProjectilePierceCount => 0,
            PropType.WeaponSlotCount => 0,
            _ => 0
        };
    }

    private void InitializeAttributes()
    {
        ClearRuntimeAttributes();
        RegisterKnownAttributes();
        RegisterMappings();
        AddBaseAttributesFromProvider();
        AddInitialModifiersFromProvider();
        ApplyEnemyProgressionModifiers();
        RecalculateAllAttributes(false);
    }

    private void ClearRuntimeAttributes()
    {
        attributeSystem.ValuesChanged -= NotifyAttributesChanged;
        attributeSystem.Clear();
        attributeSystem.ValuesChanged += NotifyAttributesChanged;
    }

    private void AddBaseAttributesFromProvider()
    {
        if (!owner.TryGetComponent(out IPropGroupProvider baseAttributeProvider))
        {
            return;
        }

        if (baseAttributeProvider.BasePropsGroup == null)
        {
            Debug.LogWarning($"{GetOwnerName()} 缺少基础属性组，将使用属性默认值。", this);
            return;
        }

        IReadOnlyList<BasePropData> values = baseAttributeProvider.BasePropsGroup.Values;
        if (values == null)
        {
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            BasePropData baseAttribute = values[i];
            attributeSystem.AddBaseValue(baseAttribute.propType, baseAttribute.value);
        }
    }

    private void AddInitialModifiersFromProvider()
    {
        if (owner.TryGetComponent(out IPropModifierProvider modifierProvider))
        {
            AddModifiers(owner.RuntimeId, modifierProvider.PropModifierDataList);
        }
    }

    private void ApplyEnemyProgressionModifiers()
    {
        if (owner is Enemy enemy)
        {
            enemy.ApplyInitialProgressionModifiers(this);
        }
    }

    private void RecalculateAllAttributes(bool notifyChanges = true, bool notifyAllWhenUnchanged = false)
    {
        attributeSystem.RecalculateAll(notifyChanges, notifyAllWhenUnchanged);
    }

    private bool IsValidMapping(PropMappingData mapping, int index)
    {
        if (!Enum.IsDefined(typeof(PropType), mapping.sourcePropType) ||
            !Enum.IsDefined(typeof(PropType), mapping.targetPropType))
        {
            Debug.LogWarning(
                $"[AttributeManager] 忽略无效属性映射 #{index}：{mapping.sourcePropType} -> {mapping.targetPropType}。",
                this);
            return false;
        }

        if (mapping.sourcePropType == mapping.targetPropType)
        {
            Debug.LogWarning(
                $"[AttributeManager] 忽略自身属性映射 #{index}：{mapping.sourcePropType}。",
                this);
            return false;
        }

        return true;
    }

    private string GetOwnerName()
    {
        return owner != null ? owner.name : name;
    }

    private void NotifyAttributesChanged()
    {
        OnAttributesChanged?.Invoke();
    }

    private void RegisterKnownAttributes()
    {
        Array values = Enum.GetValues(typeof(PropType));
        for (int i = 0; i < values.Length; i++)
        {
            PropType propType = (PropType)values.GetValue(i);
            attributeSystem.RegisterAttribute(propType, GetDefaultAttributeValue(propType));
        }
    }

    private void RegisterMappings()
    {
        if (attributeMappings == null || attributeMappings.Count == 0)
        {
            return;
        }

        List<AttributeMapping<PropType>> mappings = new(attributeMappings.Count);
        for (int i = 0; i < attributeMappings.Count; i++)
        {
            PropMappingData mapping = attributeMappings[i];
            if (!IsValidMapping(mapping, i) || mapping.conversionPercent == 0)
            {
                continue;
            }

            mappings.Add(new AttributeMapping<PropType>(
                mapping.sourcePropType,
                mapping.targetPropType,
                PercentPointsToRatioValue(mapping.conversionPercent)));
        }

        attributeSystem.SetMappings(mappings);
    }

    private static AttributeModifier<PropType> ToAttributeModifier(PropModifierData modifier)
    {
        int value = modifier.modifierType == PropModifierType.Add
            ? modifier.value
            : PercentPointsToRatioValue(modifier.value);
        return new AttributeModifier<PropType>(
            modifier.propType,
            ToAttributeModifierType(modifier.modifierType),
            value);
    }

    private static AttributeModifierType ToAttributeModifierType(PropModifierType modifierType)
    {
        return modifierType switch
        {
            PropModifierType.Add => AttributeModifierType.Add,
            PropModifierType.BaseMultiplier => AttributeModifierType.BaseMultiplier,
            PropModifierType.BonusMultiplier => AttributeModifierType.BonusMultiplier,
            PropModifierType.FinalMultiplier => AttributeModifierType.FinalMultiplier,
            _ => AttributeModifierType.Add
        };
    }

    private static int PercentPointsToRatioValue(int percentPoints)
    {
        long ratioValue = (long)percentPoints * AttributeSystem<PropType>.RATIO_SCALE / 100;
        if (ratioValue > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (ratioValue < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)ratioValue;
    }
}
