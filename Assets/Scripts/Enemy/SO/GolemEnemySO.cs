using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GolemEnemy", menuName = ScriptableObjectMenuPaths.GOLEM_ENEMY, order = 3)]
public class GolemEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Golem_Attack";
    public const string POST_CHARGE_ATTACK_ACTION_ID = "Golem_PostChargeAttack";

    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField] private EnemyActionDefinition chargeAction = new();
    [SerializeField] private EnemyActionDefinition postChargeAttackAction = new();
    [SerializeField] private EnemyActionDefinition recoveryAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.55f;

    [Header("狂暴冲锋")]
    [SerializeField, Min(0f)] private float berserkInterval = 8f;
    [SerializeField, Min(0f)] private float preChargeStunDuration = 1.2f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float chargeAnimationSpeedMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float postChargeStunDuration = 1f;
    [SerializeField, Min(0f)] private float chargeDamageRadius = 1.15f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1f;
    [SerializeField] private List<PropModifierData> chargeModifiers = new();

    [Header("攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float attackSpeedBenefitRatio = 1f;
    [SerializeField] private List<PropModifierData> postChargeAttackModifiers = new();

    public EnemyActionDefinition AttackAction
    {
        get
        {
            EnsureActionDefaults();
            return attackAction;
        }
    }

    public EnemyActionDefinition ChargeAction
    {
        get
        {
            EnsureActionDefaults();
            return chargeAction;
        }
    }

    public EnemyActionDefinition PostChargeAttackAction
    {
        get
        {
            EnsureActionDefaults();
            return postChargeAttackAction;
        }
    }

    public EnemyActionDefinition RecoveryAction
    {
        get
        {
            EnsureActionDefaults();
            return recoveryAction;
        }
    }

    public float AttackCommitNormalizedTime => AttackAction.CommitNormalizedTime;
    public float BerserkInterval => berserkInterval;
    public float PreChargeStunDuration => preChargeStunDuration;
    public float ChargeDuration => chargeDuration;
    public float ChargeAnimationSpeedMultiplier => chargeAnimationSpeedMultiplier;
    public float PostChargeStunDuration => postChargeStunDuration;
    public float ChargeDamageRadius => chargeDamageRadius;
    public float ChargeDamageMultiplier => chargeDamageMultiplier;
    public IReadOnlyList<PropModifierData> ChargeModifiers => chargeModifiers;
    public float AttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
    public IReadOnlyList<PropModifierData> PostChargeAttackModifiers => postChargeAttackModifiers;

    private void OnValidate()
    {
        berserkInterval = Mathf.Max(0f, berserkInterval);
        preChargeStunDuration = Mathf.Max(0f, preChargeStunDuration);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        chargeAnimationSpeedMultiplier = Mathf.Max(0.01f, chargeAnimationSpeedMultiplier);
        postChargeStunDuration = Mathf.Max(0f, postChargeStunDuration);
        chargeDamageRadius = Mathf.Max(0f, chargeDamageRadius);
        chargeDamageMultiplier = Mathf.Max(0f, chargeDamageMultiplier);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        EnsureActionDefaults();
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        chargeAction ??= new EnemyActionDefinition();
        postChargeAttackAction ??= new EnemyActionDefinition();
        recoveryAction ??= new EnemyActionDefinition();

        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        string moveStateName = AnimConfig != null ? AnimConfig.Move : "Move";
        string idleStateName = AnimConfig != null ? AnimConfig.Idle : "Idle";
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
        chargeAction.ConfigureDefaults(
            "Golem_Charge",
            moveStateName,
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            chargeDuration);
        postChargeAttackAction.ConfigureDefaults(POST_CHARGE_ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
        recoveryAction.ConfigureDefaults(
            "Golem_Recovery",
            idleStateName,
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            postChargeStunDuration);
    }
}
