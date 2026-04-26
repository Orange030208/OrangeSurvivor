using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NovaCasterEnemy", menuName = "Enemy/NovaCasterEnemy", order = 5)]
public class NovaCasterEnemySO : EnemySO
{
    [Header("Cast")]
    [Min(0f)] public float novaTriggerDistance = 3.5f;
    [Min(0.1f)] public float novaCooldown = 4f;
    
    public List<PropModifierData> novaModifiers;

    [Header("Strategies")]
    public MovementStrategyBase repositionMoveStrategy;
    public MovementStrategyBase combatMoveStrategy;
    public AttackStrategyBase primaryAttackStrategy;
    public AttackStrategyBase novaAttackStrategy;
}
