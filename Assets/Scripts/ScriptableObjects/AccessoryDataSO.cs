using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Accessory Data", menuName = "SO/Accessory", order = 0)]
public class AccessoryDataSO : ScriptableObject, IEnumerable<PropKV>
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public int Price { get; private set; }

    [field: Range(0, 3)]
    [field: SerializeField]
    public int Rarity { get; private set; }

    [SerializeField] private PropKV[] modifiers;

    public IEnumerator<PropKV> GetEnumerator()
    {
        foreach (var modifier in modifiers)
        {
            yield return modifier;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Dictionary<PropType, float> ToDictionary()
    {
        Dictionary<PropType, float> ret = new Dictionary<PropType, float>();
        foreach (var modifier in modifiers)
        {
            ret.Add(modifier.propType, modifier.value);
        }
        
        return ret;
    }
}

[Serializable]
public struct PropKV
{
    public PropType propType;
    public float value;


    public PropKV(PropType propType, float value)
    {
        this.propType = propType;
        this.value = value;
    }
}