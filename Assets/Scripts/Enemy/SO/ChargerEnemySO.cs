using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChargerEnemy", menuName = ScriptableObjectMenuPaths.CHARGER_ENEMY, order = 3)]
public class ChargerEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "ChargerEnemy_Attack";
    public const string CHARGE_ACTION_ID = "ChargerEnemy_Charge";

    [Header("动作时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField] private EnemyActionDefinition chargeAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.55f;

    [Header("冲撞")]
    [SerializeField, Min(0f)] private float chargeInterval = 8f;
    [SerializeField, Min(0f)] private float preChargeDuration = 1.2f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.75f;
    [SerializeField, Min(0f)] private float chargeDamageRadius = 1.15f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1f;
    [SerializeField] private List<PropModifierData> chargeModifiers = new();

    [Header("攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float attackSpeedBenefitRatio = 1f;

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

    public float AttackCommitNormalizedTime => AttackAction.CommitNormalizedTime;
    public float ChargeInterval => chargeInterval;
    public float PreChargeDuration => preChargeDuration;
    public float ChargeDuration => chargeDuration;
    public float ChargeDamageRadius => chargeDamageRadius;
    public float ChargeDamageMultiplier => chargeDamageMultiplier;
    public IReadOnlyList<PropModifierData> ChargeModifiers => chargeModifiers;
    public float AttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);

    private void OnValidate()
    {
        chargeInterval = Mathf.Max(0f, chargeInterval);
        preChargeDuration = Mathf.Max(0f, preChargeDuration);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        chargeDamageRadius = Mathf.Max(0f, chargeDamageRadius);
        chargeDamageMultiplier = Mathf.Max(0f, chargeDamageMultiplier);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        EnsureActionDefaults();
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        chargeAction ??= new EnemyActionDefinition();

        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        string chargeStateName = AnimConfig != null ? AnimConfig.Charge : "Charge";
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
        chargeAction.ConfigureDefaults(
            CHARGE_ACTION_ID,
            chargeStateName,
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            preChargeDuration);
    }
}
