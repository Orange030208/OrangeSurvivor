using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MageEnemy", menuName = ScriptableObjectMenuPaths.MAGE_ENEMY, order = 0)]
public class MageEnemySO : EnemySO
{
    [Header("State Thresholds")]
    [Range(0f, 100f)] 
    public float lowHpPercent = 30f;
    
    public List<PropModifierData> fastBurstModifierData = new List<PropModifierData>();

    [Header("Strategies")]
    public MovementStrategyBase normalMovementStrategy;
    public AttackStrategyBase normalAttackStrategy;
    public MovementStrategyBase retreatMovementStrategy;
}
