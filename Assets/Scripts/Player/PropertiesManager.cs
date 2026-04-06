using System;
using System.Collections.Generic;
using UnityEngine;

public class PropertiesManager : MonoBehaviour
{
    [SerializeField] private CharacterDataSO basePropsData;

    private Dictionary<PropType, float> baseProps = new();
    private readonly Dictionary<PropType, float> bonusProps = new();
    private readonly Dictionary<string, Dictionary<PropType, float>> bonusSources = new();

    public event Action<PropType, float> OnPropertyChanged;
    public event Action OnAllPropertiesChanged;

    private void Awake()
    {
        InitializeBaseProps();
    }

    private void Start()
    {
        NotifyAllPropertiesChanged();
    }

    private void InitializeBaseProps()
    {
        baseProps = basePropsData != null
            ? basePropsData.GetBaseProps()
            : new Dictionary<PropType, float>();

        bonusProps.Clear();
        foreach (var prop in baseProps)
        {
            bonusProps[prop.Key] = 0;
        }
    }

    public void AddBonusModifier(string sourceId, PropType propType, float value)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[PropertiesManager] AddBonusModifier: sourceId is null or empty");
            return;
        }

        Debug.Log($"[PropertiesManager] AddBonusModifier: sourceId={sourceId}, propType={propType}, value={value}");

        if (!bonusSources.ContainsKey(sourceId))
        {
            bonusSources[sourceId] = new Dictionary<PropType, float>();
        }

        bonusSources[sourceId][propType] = value;
        RecalculateBonus(propType);
    }

    public void RemoveBonusModifier(string sourceId, PropType propType)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (bonusSources.ContainsKey(sourceId))
        {
            bonusSources[sourceId].Remove(propType);
            if (bonusSources[sourceId].Count == 0)
            {
                bonusSources.Remove(sourceId);
            }
        }

        RecalculateBonus(propType);
    }

    public void RemoveAllBonusModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!bonusSources.ContainsKey(sourceId))
        {
            return;
        }

        var affectedTypes = new List<PropType>(bonusSources[sourceId].Keys);
        bonusSources.Remove(sourceId);

        foreach (var propType in affectedTypes)
        {
            RecalculateBonus(propType);
        }
    }

    private void RecalculateBonus(PropType propType)
    {
        float oldValue = bonusProps.GetValueOrDefault(propType, 0);
        float newValue = 0;

        foreach (var source in bonusSources.Values)
        {
            if (source.TryGetValue(propType, out float value))
            {
                newValue += value;
            }
        }

        bonusProps[propType] = newValue;

        if (Mathf.Abs(oldValue - newValue) > Mathf.Epsilon)
        {
            OnPropertyChanged?.Invoke(propType, GetPropValue(propType));
        }
    }

    private void NotifyAllPropertiesChanged()
    {
        OnAllPropertiesChanged?.Invoke();
    }

    public float GetPropValue(PropType propType)
    {
        return GetBaseValue(propType) + GetBonusValue(propType);
    }

    public float GetBaseValue(PropType propType)
    {
        return baseProps.GetValueOrDefault(propType, 0);
    }

    public float GetBonusValue(PropType propType)
    {
        return bonusProps.GetValueOrDefault(propType, 0);
    }

    public Dictionary<PropType, float> GetAllPropValues()
    {
        var result = new Dictionary<PropType, float>(baseProps.Count);
        foreach (var prop in baseProps)
        {
            result[prop.Key] = GetPropValue(prop.Key);
        }

        return result;
    }
}
