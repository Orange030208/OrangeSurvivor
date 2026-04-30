using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlyForestEnemy", menuName = ScriptableObjectMenuPaths.FLY_FOREST, order = 0)]
public class FlyForestEnemySO : EnemySO
{
    [Header("State Thresholds")]
    [Range(0f, 100f)] 
    public float lowHpPercent = 30f;
    
    public List<PropModifierData> fastBurstModifierData = new List<PropModifierData>();

    [Header("Attacks")]
    public EnemyAttackDefinitionSO normalAttackDefinition;

    [Header("Strategies")]
    public MovementStrategyBase normalMovementStrategy;
    public MovementStrategyBase retreatMovementStrategy;

    public override IReadOnlyList<EnemyAttackDefinitionSO> GetAttackDefinitions()
    {
        List<EnemyAttackDefinitionSO> results = new();
        AddAttackDefinition(results, normalAttackDefinition);
        return results;
    }
}
