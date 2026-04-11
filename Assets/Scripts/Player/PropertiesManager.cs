using System;
using System.Collections.Generic;
using UnityEngine;

public class PropertiesManager : MonoBehaviour
{
    [SerializeField] private CharacterDataSO basePropsData;

    private Dictionary<PropType, float> baseProps = new();
    private readonly Dictionary<PropType, float> calculatedProps = new();
    private readonly Dictionary<string, List<PropEntry>> modifierSources = new();
    private FeatureHost featureHost;

    public CharacterDataSO CharacterData => basePropsData;

    public event Action<PropType, float> OnPropertyChanged;
    public event Action OnAllPropertiesChanged;

    private void Awake()
    {
        featureHost = GetComponent<FeatureHost>();
        InitializeBaseProps();
    }

    private void Start()
    {
        ApplyCharacterFeatures();
        ApplyInitialLoadout();
        NotifyAllPropertiesChanged();
    }

    private void OnDisable()
    {
        if (featureHost != null)
        {
            FeatureInstaller.RemoveSource(featureHost, FeatureInstaller.CharacterSourceId);
        }
    }

    private void InitializeBaseProps()
    {
        baseProps = basePropsData != null
            ? basePropsData.GetBaseProps()
            : CharacterDataSO.CreateSharedBaseProps();

        RecalculateAllProps(false);
    }

    private void ApplyCharacterFeatures()
    {
        if (basePropsData == null || featureHost == null)
        {
            return;
        }

        FeatureInstaller.InstallCharacter(featureHost, basePropsData);
    }

    private void ApplyInitialLoadout()
    {
        if (basePropsData == null)
        {
            return;
        }

        WeaponsHolder weaponsHolder = GetComponent<WeaponsHolder>();
        if (weaponsHolder != null)
        {
            foreach (var entry in basePropsData.InitialWeapons)
            {
                if (entry.weaponData == null)
                {
                    continue;
                }

                weaponsHolder.AddWeapon(entry.weaponData, entry.level);
            }
        }

        AccessoryManager accessoryManager = GetComponent<AccessoryManager>();
        if (accessoryManager != null)
        {
            foreach (var accessory in basePropsData.InitialAccessories)
            {
                if (accessory == null)
                {
                    continue;
                }

                accessoryManager.EquipAccessory(accessory);
            }
        }
    }

    public void AddBonusModifier(string sourceId, PropType propType, float value)
    {
        AddModifier(sourceId, new PropEntry(propType, value));
    }

    public void AddModifier(string sourceId, PropEntry modifier)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            Debug.LogWarning("[PropertiesManager] AddModifier: sourceId is null or empty");
            return;
        }

        AddModifiers(sourceId, new List<PropEntry> { modifier });
    }

    public void AddModifiers(string sourceId, IReadOnlyList<PropEntry> modifiers)
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

        modifierSources[sourceId] = new List<PropEntry>(modifiers);
        RecalculateAllProps();
    }

    public void RemoveBonusModifier(string sourceId, PropType propType)
    {
        RemoveModifier(sourceId, propType, PropModifierType.Flat);
    }

    public void RemoveModifier(string sourceId, PropType propType, PropModifierType modifierType)
    {
        if (string.IsNullOrWhiteSpace(sourceId) || !modifierSources.TryGetValue(sourceId, out List<PropEntry> modifiers))
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

    public void RemoveAllBonusModifiers(string sourceId)
    {
        RemoveAllModifiers(sourceId);
    }

    public void RemoveAllModifiers(string sourceId)
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
        var changedProps = notifyChanges ? new List<PropType>() : null;

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
        float flat = 0f;
        float basePercent = 0f;
        float finalFlat = 0f;
        float finalPercent = 0f;

        foreach (var source in modifierSources.Values)
        {
            for (int i = 0; i < source.Count; i++)
            {
                PropEntry entry = source[i];
                if (entry.propType != propType)
                {
                    continue;
                }

                switch (entry.modifierType)
                {
                    case PropModifierType.Flat:
                        flat += entry.value;
                        break;
                    case PropModifierType.BasePercent:
                        basePercent += entry.value;
                        break;
                    case PropModifierType.FinalFlat:
                        finalFlat += entry.value;
                        break;
                    case PropModifierType.FinalPercent:
                        finalPercent += entry.value;
                        break;
                }
            }
        }

        float result = baseValue + flat;
        result += baseValue * basePercent;
        result += finalFlat;
        result *= 1f + finalPercent;
        return result;
    }

    private void NotifyAllPropertiesChanged()
    {
        OnAllPropertiesChanged?.Invoke();
    }

    public float GetPropValue(PropType propType)
    {
        return calculatedProps.GetValueOrDefault(propType, baseProps.GetValueOrDefault(propType, 0f));
    }

    public float GetBaseValue(PropType propType)
    {
        return baseProps.GetValueOrDefault(propType, 0f);
    }

    public float GetBonusValue(PropType propType)
    {
        return GetPropValue(propType) - GetBaseValue(propType);
    }

    public Dictionary<PropType, float> GetAllPropValues()
    {
        var result = new Dictionary<PropType, float>(calculatedProps.Count);
        foreach (var prop in calculatedProps)
        {
            result[prop.Key] = prop.Value;
        }

        return result;
    }
}
