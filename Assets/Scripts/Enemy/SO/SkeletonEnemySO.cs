using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;
    [SerializeField, Range(0f, 1f)] private float attackFinishNormalizedTime = 0.95f;

    [Header("Attacks")]
    [FormerlySerializedAs("defaultAttackDefinition")]
    [SerializeField] private EnemyAttackDefinitionSO attackDefinition;

    [Header("Strategies")]
    public MovementStrategyBase chaseMoveStrategy;

    public float AttackCommitNormalizedTime => attackCommitNormalizedTime;
    public float AttackFinishNormalizedTime => Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
    public EnemyAttackDefinitionSO AttackDefinition => attackDefinition;

    public override IReadOnlyList<EnemyAttackDefinitionSO> GetAttackDefinitions()
    {
        List<EnemyAttackDefinitionSO> results = new();
        AddAttackDefinition(results, attackDefinition);
        return results;
    }

    private void OnValidate()
    {
        attackFinishNormalizedTime = Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
    }
}
