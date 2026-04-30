using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WormEnemy", menuName = ScriptableObjectMenuPaths.WORM_ENEMY, order = 1)]
public class WormEnemySO : EnemySO
{
    [Header("Distance")]
    [Min(0f)] public float retreatTriggerDistance = 4f;
    [Min(0f)] public float retreatCompleteDistance = 7f;
    
    [Header("攻击时机")]
    [Range(0f, 1f)] public float attackCommitNormalizedTime = 0.5f;
    [Range(0f, 1f)] public float attackFinishNormalizedTime = 0.95f;

    [Header("Attacks")]
    [FormerlySerializedAs("defaultAttackDefinition")]
    public EnemyAttackDefinitionSO attackDefinition;
    public EnemyAttackDefinitionSO retreatAttackDefinition;

    [Header("Strategies")]
    public MovementStrategyBase approachMoveStrategy;
    public MovementStrategyBase retreatMoveStrategy;

    public override IReadOnlyList<EnemyAttackDefinitionSO> GetAttackDefinitions()
    {
        List<EnemyAttackDefinitionSO> results = new();
        AddAttackDefinition(results, attackDefinition);
        AddAttackDefinition(results, retreatAttackDefinition);
        return results;
    }

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        retreatCompleteDistance = Mathf.Max(retreatTriggerDistance, retreatCompleteDistance);
    }
}
