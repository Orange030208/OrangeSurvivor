using System;
using System.Collections.Generic;
using Survivors.Accessory;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Accessory Data", menuName = "SO/Accessory", order = 0)]
public class AccessoryDataSO : ItemDataSO
{
    [SerializeField] protected string accessoryId;
    [SerializeField] protected int recyclePrice;

    [Range(0, 3)]
    [SerializeField]
    private int rarity;

    [SerializeField] private List<PropertyModifierEntry> propertyModifiers = new();
    [SerializeReference] private List<AccessoryEffectBase> customEffects = new();

    public string AccessoryId => accessoryId;
    public int RecyclePrice => recyclePrice;
    public int Rarity => rarity;

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
        var dictionary = new Dictionary<PropType, float>();
        foreach (var modifier in propertyModifiers)
        {
            dictionary[modifier.propType] = modifier.value;
        }

        return dictionary;
    }

    public List<IAccessoryEffect> CreateEffects(string instanceId)
    {
        var effects = new List<IAccessoryEffect>();
        string effectPrefix = $"ACC_{accessoryId}_{instanceId}";

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

    [Serializable]
    private struct PropertyModifierEntry
    {
        public PropType propType;
        public float value;
    }
}
