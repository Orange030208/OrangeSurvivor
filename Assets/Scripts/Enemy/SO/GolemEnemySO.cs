using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GolemEnemy", menuName = ScriptableObjectMenuPaths.GOLEM_ENEMY, order = 3)]
public class GolemEnemySO : EnemySO
{
    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.55f;
    [SerializeField, Range(0f, 1f)] private float attackFinishNormalizedTime = 0.95f;

    [Header("Berserk Charge")]
    [SerializeField, Min(0f)] private float berserkInterval = 8f;
    [SerializeField, Min(0f)] private float preChargeStunDuration = 1.2f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float chargeAnimationSpeedMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float postChargeStunDuration = 1f;
    [SerializeField, Min(0f)] private float chargeDamageRadius = 1.15f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1f;
    [SerializeField] private List<PropModifierData> chargeModifiers = new();

    [Header("Attacks")]
    [SerializeField] private EnemyAttackDefinitionSO attackDefinition;
    [SerializeField] private CircleAttackHitShapeSO postChargeAttackHitShape;
    [SerializeField, Min(0f)] private float postChargeAttackRangeMultiplier = 1.5f;

    [Header("Strategies")]
    public MovementStrategyBase chaseMoveStrategy;

    public float AttackCommitNormalizedTime => attackCommitNormalizedTime;
    public float AttackFinishNormalizedTime => Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
    public float BerserkInterval => berserkInterval;
    public float PreChargeStunDuration => preChargeStunDuration;
    public float ChargeDuration => chargeDuration;
    public float ChargeAnimationSpeedMultiplier => chargeAnimationSpeedMultiplier;
    public float PostChargeStunDuration => postChargeStunDuration;
    public float ChargeDamageRadius => chargeDamageRadius;
    public float ChargeDamageMultiplier => chargeDamageMultiplier;
    public IReadOnlyList<PropModifierData> ChargeModifiers => chargeModifiers;
    public EnemyAttackDefinitionSO AttackDefinition => attackDefinition;
    public CircleAttackHitShapeSO PostChargeAttackHitShape => postChargeAttackHitShape;
    public float PostChargeAttackRangeMultiplier => Mathf.Max(0f, postChargeAttackRangeMultiplier);

    public override IReadOnlyList<EnemyAttackDefinitionSO> GetAttackDefinitions()
    {
        List<EnemyAttackDefinitionSO> results = new();
        AddAttackDefinition(results, attackDefinition);
        return results;
    }

    private void OnValidate()
    {
        attackFinishNormalizedTime = Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
        berserkInterval = Mathf.Max(0f, berserkInterval);
        preChargeStunDuration = Mathf.Max(0f, preChargeStunDuration);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        chargeAnimationSpeedMultiplier = Mathf.Max(0.01f, chargeAnimationSpeedMultiplier);
        postChargeStunDuration = Mathf.Max(0f, postChargeStunDuration);
        chargeDamageRadius = Mathf.Max(0f, chargeDamageRadius);
        chargeDamageMultiplier = Mathf.Max(0f, chargeDamageMultiplier);
        postChargeAttackRangeMultiplier = Mathf.Max(0f, postChargeAttackRangeMultiplier);
    }
}
