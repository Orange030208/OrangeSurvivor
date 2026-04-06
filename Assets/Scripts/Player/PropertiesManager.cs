using System;
using System.Collections.Generic;
using UnityEngine;

public class PropertiesManager : MonoBehaviour
{
    [SerializeField] private CharacterDataSO basePropsData;

    private Dictionary<PropType, float> baseProps = new();
    private readonly Dictionary<PropType, float> addens = new();
    private readonly Dictionary<string, Dictionary<PropType, float>> additiveSources = new();

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
        addens.Clear();

        foreach (var prop in baseProps)
        {
            addens[prop.Key] = 0;
        }
    }

    public void AddAdditiveModifier(string sourceId, PropType propType, float value)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!additiveSources.ContainsKey(sourceId))
        {
            additiveSources[sourceId] = new Dictionary<PropType, float>();
        }

        additiveSources[sourceId][propType] = value;
        RecalculateAdditive(propType);
    }

    public void RemoveAdditiveModifier(string sourceId, PropType propType)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (additiveSources.ContainsKey(sourceId))
        {
            additiveSources[sourceId].Remove(propType);
            if (additiveSources[sourceId].Count == 0)
            {
                additiveSources.Remove(sourceId);
            }
        }
        RecalculateAdditive(propType);
    }

    public void RemoveAllAdditiveModifiers(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return;
        }

        if (!additiveSources.ContainsKey(sourceId)) return;

        var affectedTypes = new List<PropType>(additiveSources[sourceId].Keys);
        additiveSources.Remove(sourceId);

        foreach (var propType in affectedTypes)
        {
            RecalculateAdditive(propType);
        }
    }

    private void RecalculateAdditive(PropType propType)
    {
        float oldValue = addens.GetValueOrDefault(propType, 0);
        float newValue = 0;

        foreach (var source in additiveSources.Values)
        {
            if (source.TryGetValue(propType, out float value))
            {
                newValue += value;
            }
        }

        addens[propType] = newValue;

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
        float baseValue = baseProps.GetValueOrDefault(propType, 0);
        float additiveValue = addens.GetValueOrDefault(propType, 0);
        return baseValue + additiveValue;
    }

    public float GetBaseValue(PropType propType)
    {
        return baseProps.GetValueOrDefault(propType, 0);
    }

    public float GetAdditiveValue(PropType propType)
    {
        return addens.GetValueOrDefault(propType, 0);
    }
}
