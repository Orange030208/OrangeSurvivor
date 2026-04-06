using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Data", menuName = "SO/WeaponData", order = 0)]
public class WeaponDataSO : ScriptableObject
{
    [field:SerializeField] public string Name { get; private set; }
    [field:SerializeField]public Sprite Icon { get; private set; }
    [field:SerializeField]public int PurchasePrice { get; private set; }
    [field:SerializeField]public Weapon WeaponPrefab{ get; private set; }

    [Header("属性")] [SerializeField] private float attack;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float criticalChance;
    [SerializeField] private float criticalPercent;
    [SerializeField] private float range;

    
    public Dictionary<PropType, float> GetBaseProps()
    {
        return new Dictionary<PropType, float>
        {
            { PropType.Attack, attack },
            { PropType.AttackSpeed, attackSpeed },
            { PropType.CriticalChance, criticalChance },
            { PropType.CriticalPercent, criticalPercent },
            { PropType.Range, range },
        };
    }
    
    //TODO:不要重复构建字典
    public float GetPropValue(PropType propType)
    {
        if (GetBaseProps().TryGetValue(propType, out float value))
        {
            return value;
        }
        else
        {
            Debug.Log($"{Name}不存在属性{propType.ToString()}");
            return 0;
        }
    }
}