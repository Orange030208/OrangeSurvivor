using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Data", menuName = "SO/CharacterData", order = 0)]
public class CharacterDataSO : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public int PurchasePrice { get; private set; }

    [Header("角色属性")]
    [SerializeField]private float attack;
    [SerializeField]private float attackSpeed;
    [SerializeField]private float criticalChance;
    [SerializeField]private float criticalPercent;
    [SerializeField]private float moveSpeed;
    [SerializeField]private float maxHealth;
    [SerializeField]private float range;
    [SerializeField]private float healthRecoverySpeed;
    [SerializeField]private float armor;
    [SerializeField]private float luck;
    [SerializeField]private float dodge;
    [SerializeField]private float lifeSteal;

    public Dictionary<EntityPropType, float> GetBaseProps()
    {
        return new Dictionary<EntityPropType, float>
        {
            { EntityPropType.Attack, attack },
            { EntityPropType.AttackSpeed, attackSpeed },
            { EntityPropType.CriticalChance, criticalChance },
            { EntityPropType.CriticalPercent, criticalPercent },
            { EntityPropType.MoveSpeed, moveSpeed },
            { EntityPropType.MaxHealth, maxHealth },
            { EntityPropType.Range, range },
            { EntityPropType.HealthRecoverySpeed, healthRecoverySpeed },
            { EntityPropType.Armor, armor },
            { EntityPropType.Luck, luck },
            { EntityPropType.Dodge, dodge },
            { EntityPropType.LifeSteal, lifeSteal },
        };
    }
}
