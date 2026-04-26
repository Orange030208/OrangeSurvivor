using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PropertiesManager : EntityComponentBase, IDescribable
{
    private Entity owner;

    private readonly Dictionary<PropType, float> baseProps = new();
    private readonly Dictionary<PropType, float> addProps = new();
    private readonly Dictionary<PropType, float> baseOnlyMultiplierProps = new();
    private readonly Dictionary<PropType, float> bonusMultiplierProps = new();
    private readonly Dictionary<PropType, float> finalMultiplierProps = new();
    private readonly Dictionary<PropType, float> calculatedProps = new();
    private readonly Dictionary<string, List<PropModifierData>> modifierSources = new();

    public string Title => "属性";
    public Sprite Icon => null;
    public string Description => "属性管理器";

    public IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        foreach (KeyValuePair<PropType, float> info in calculatedProps)
        {
            infos.Add(new DescriptorInfo(info.Key.GetChineseName(),
                info.Key.BuildIconNameValueDescription(info.Value)));
        }

        return infos;
    }

    public event Action<PropType, float> OnPropertyChanged;
    public event Action OnAllPropertiesChanged;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;

        InitializeProps();
        NotifyAllPropertiesChanged();
    }

    private void Clear()
    {
        baseProps.Clear();
        addProps.Clear();
        baseOnlyMultiplierProps.Clear();
        bonusMultiplierProps.Clear();
        finalMultiplierProps.Clear();
        calculatedProps.Clear();
    }

    private void InitializeProps()
    {
        Clear();
        if (!this.owner.TryGetComponent<IPropGroupProvider>(out IPropGroupProvider basePropProvider))
        {
            Debug.LogWarning($"{owner.name}应该实现IPropGroupProvider为PropertiesManager提供基础属性");
        }

        IReadOnlyList<BasePropData> values = basePropProvider.BasePropsGroup.Values;
        for (int i = 0; i < values.Count; i++)
        {
            BasePropData baseProp = values[i];
            AddValue(baseProps, baseProp.propType, baseProp.value);
        }
        
        if (!this.owner.TryGetComponent<IPropModifierProvider>(out IPropModifierProvider propModifierProvider))
        {
            Debug.Log($"{owner.name}没有提供额外属性");
        }
        else
        {
            var propModifierDataList = propModifierProvider.PropModifierDataList;
            AddModifiers(owner.RuntimeId, propModifierDataList);
        }

        RecalculateAllProps(false);
    }

    public void AddModifier(string sourceId, PropModifierData modifier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[PropertiesManager] AddModifier: sourceId is null or empty");
            return;
        }

        AddModifiers(sourceId, new List<PropModifierData> { modifier });
    }

    public void AddModifiers(string sourceId, IReadOnlyList<PropModifierData> modifiers)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[PropertiesManager] AddModifiers: sourceId is null or empty");
            return;
        }

        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        modifierSources[sourceId] = new List<PropModifierData>(modifiers);
        RecalculateAllProps();
    }

    public void RemoveModifier(string sourceId, PropType propType, PropModifierType modifierType)
    {
        if (string.IsNullOrWhiteSpace(sourceId) ||
            !modifierSources.TryGetValue(sourceId, out List<PropModifierData> modifiers))
        {
            return;
        }

        modifiers.RemoveAll(entry => entry.propType == propType && entry.modifierType == modifierType);
        if (modifiers.Count == 0)
        {
            modifierSources.Remove(sourceId);
        }

        RecalculateAllProps();
    }

    public void RemoveModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!modifierSources.Remove(sourceId))
        {
            return;
        }

        RecalculateAllProps();
    }

    private void RecalculateAllProps(bool notifyChanges = true)
    {
        List<PropType> changedProps = notifyChanges ? new List<PropType>() : null;

        Array values = Enum.GetValues(typeof(PropType));
        for (int i = 0; i < values.Length; i++)
        {
            PropType propType = (PropType)values.GetValue(i);
            float oldValue = calculatedProps.GetValueOrDefault(propType, 0f);
            float newValue = CalculateFinalValue(propType);
            calculatedProps[propType] = newValue;

            if (notifyChanges && Mathf.Abs(oldValue - newValue) > Mathf.Epsilon)
            {
                changedProps.Add(propType);
            }
        }

        if (!notifyChanges)
        {
            return;
        }

        for (int i = 0; i < changedProps.Count; i++)
        {
            PropType propType = changedProps[i];
            OnPropertyChanged?.Invoke(propType, calculatedProps[propType]);
        }

        if (changedProps.Count > 0)
        {
            NotifyAllPropertiesChanged();
        }
    }

    private float CalculateFinalValue(PropType propType)
    {
        float baseValue = baseProps.GetValueOrDefault(propType, 0f);
        float addValue = addProps.GetValueOrDefault(propType, 0f);
        float baseOnlyMultiplierValue = baseOnlyMultiplierProps.GetValueOrDefault(propType, 0f);
        float bonusMultiplierValue = bonusMultiplierProps.GetValueOrDefault(propType, 0f);
        float finalMultiplierValue = finalMultiplierProps.GetValueOrDefault(propType, 0f);

        foreach (List<PropModifierData> source in modifierSources.Values)
        {
            for (int i = 0; i < source.Count; i++)
            {
                PropModifierData entry = source[i];
                if (entry.propType != propType)
                {
                    continue;
                }

                switch (entry.modifierType)
                {
                    case PropModifierType.Add:
                        addValue += entry.value;
                        break;
                    case PropModifierType.BaseMultiplier:
                        baseOnlyMultiplierValue += entry.value;
                        break;
                    case PropModifierType.BonusMultiplier:
                        bonusMultiplierValue += entry.value;
                        break;
                    case PropModifierType.FinalMultiplier:
                        finalMultiplierValue += entry.value;
                        break;
                }
            }
        }

        float baseValueAfterMultiplier = baseValue * (1f + baseOnlyMultiplierValue);
        float bonusValue = addValue * (1f + bonusMultiplierValue);
        float result = baseValueAfterMultiplier + bonusValue;
        result *= 1f + finalMultiplierValue;
        return result;
    }

    private static void AddValue(Dictionary<PropType, float> target, PropType propType, float value)
    {
        if (target.TryGetValue(propType, out float currentValue))
        {
            target[propType] = currentValue + value;
            return;
        }

        target[propType] = value;
    }

    private void NotifyAllPropertiesChanged()
    {
        OnAllPropertiesChanged?.Invoke();
    }

    public float GetPropValue(PropType propType)
    {
        return calculatedProps.GetValueOrDefault(propType, 0f);
    }

    public float GetBaseValue(PropType propType)
    {
        return baseProps.GetValueOrDefault(propType, 0f);
    }

    public Dictionary<PropType, float> GetAllPropValues()
    {
        Dictionary<PropType, float> result = new(calculatedProps.Count);
        foreach (KeyValuePair<PropType, float> prop in calculatedProps)
        {
            result[prop.Key] = prop.Value;
        }

        return result;
    }
}