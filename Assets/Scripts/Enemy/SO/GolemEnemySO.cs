using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GolemEnemy", menuName = ScriptableObjectMenuPaths.GOLEM_ENEMY, order = 3)]
public class GolemEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Golem_Attack";
    public const string POST_CHARGE_ATTACK_ACTION_ID = "Golem_PostChargeAttack";

    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.55f;

    [Header("Berserk Charge")]
    [SerializeField, Min(0f)] private float berserkInterval = 8f;
    [SerializeField, Min(0f)] private float preChargeStunDuration = 1.2f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float chargeAnimationSpeedMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float postChargeStunDuration = 1f;
    [SerializeField, Min(0f)] private float chargeDamageRadius = 1.15f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1f;
    [SerializeField] private List<PropModifierData> chargeModifiers = new();

    [Header("Attack")]
    [SerializeField, Min(0.01f)] private float attackSpeedBenefitRatio = 1f;
    [SerializeField] private List<PropModifierData> postChargeAttackModifiers = new();

    public float AttackCommitNormalizedTime => attackCommitNormalizedTime;
    public float BerserkInterval => berserkInterval;
    public float PreChargeStunDuration => preChargeStunDuration;
    public float ChargeDuration => chargeDuration;
    public float ChargeAnimationSpeedMultiplier => chargeAnimationSpeedMultiplier;
    public float PostChargeStunDuration => postChargeStunDuration;
    public float ChargeDamageRadius => chargeDamageRadius;
    public float ChargeDamageMultiplier => chargeDamageMultiplier;
    public IReadOnlyList<PropModifierData> ChargeModifiers => chargeModifiers;
    public float AttackSpeedBenefitRatio => Mathf.Max(0.01f, attackSpeedBenefitRatio);
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
        attackSpeedBenefitRatio = Mathf.Max(0.01f, attackSpeedBenefitRatio);
    }
}
