using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlyForestEnemy", menuName = ScriptableObjectMenuPaths.FLY_FOREST, order = 0)]
public class FlyForestEnemySO : EnemySO
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
