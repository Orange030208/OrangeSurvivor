using System;
using System.Collections;
using System.Collections.Generic;
using Survivors.Accessory;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = "SO/Accessory", order = 0)]
public class AccessoryDataSO : ScriptableObject
{
    [field: SerializeField] public string AccessoryId { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public int Price { get; private set; } = 10;
    [field: SerializeField] public int RecyclePrice { get; private set; }

    [field: Range(0, 3)]
    [field: SerializeField]
    public int Rarity { get; private set; }

    [SerializeField] private List<PropertyModifierEntry> propertyModifiers = new();
    [SerializeReference] private List<AccessoryEffectBase> customEffects = new();

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(AccessoryId))
        {
            AccessoryId = Guid.NewGuid().ToString("N")[..8];
        }
    }

    public List<IAccessoryEffect> CreateEffects(string instanceId)
    {
        var effects = new List<IAccessoryEffect>();
        string effectPrefix = $"ACC_{AccessoryId}_{instanceId}";

        foreach (var modifier in propertyModifiers)
        {
            string effectId = $"{effectPrefix}_{modifier.propType}";
            effects.Add(new PropertyModifierEffect(effectId, effectId, modifier.propType, modifier.value));
        }

        foreach (var customEffect in customEffects)
        {
            if (customEffect != null)
            {
                effects.Add(customEffect);
            }
        }

        return effects;
    }

    public Dictionary<PropType, float> GetPropertyModifiers()
    {
        var dictionary = new Dictionary<PropType, float>();
        foreach (var modifier in propertyModifiers)
        {
            dictionary[modifier.propType] = modifier.value;
        }
        return dictionary;
    }

    [Serializable]
    private struct PropertyModifierEntry
    {
        public PropType propType;
        public float value;
    }
}
