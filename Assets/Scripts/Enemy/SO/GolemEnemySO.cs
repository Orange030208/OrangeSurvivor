using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GolemEnemy", menuName = ScriptableObjectMenuPaths.GOLEM_ENEMY, order = 3)]
public class GolemEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Golem_Attack";

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

    [Header("Attack")]
    [SerializeField] private DirectDamageAttackData attackConfig = new()
    {
        timing = new AttackTimingData
        {
            actionId = ATTACK_ACTION_ID,
            attackSfxKey = AudioSfxKey.Slap,
            cooldown = 1f,
            damageMultiplier = 1f,
        },
        detection = new ForwardCircleDetectionData
        {
            rangeSource = AttackRangeSource.AttackRangeProp,
            fixedRange = 1f,
            rangeMultiplier = 1f,
            forwardOffset = 0f,
        },
    };
    [SerializeField, Min(0f)] private float postChargeAttackForwardOffset = 0f;
    [SerializeField] private List<PropModifierData> postChargeAttackModifiers = new();

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
    public DirectDamageAttackData AttackConfig => attackConfig;
    public float PostChargeAttackForwardOffset => Mathf.Max(0f, postChargeAttackForwardOffset);
    public IReadOnlyList<PropModifierData> PostChargeAttackModifiers => postChargeAttackModifiers;

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
        attackConfig.timing.actionId = string.IsNullOrWhiteSpace(attackConfig.timing.actionId) ? ATTACK_ACTION_ID : attackConfig.timing.actionId;
        attackConfig.timing.cooldown = Mathf.Max(0f, attackConfig.timing.cooldown);
        attackConfig.timing.damageMultiplier = Mathf.Max(0f, attackConfig.timing.damageMultiplier);
        attackConfig.detection.fixedRange = Mathf.Max(0f, attackConfig.detection.fixedRange);
        attackConfig.detection.rangeMultiplier = Mathf.Max(0f, attackConfig.detection.rangeMultiplier);
        attackConfig.detection.forwardOffset = Mathf.Max(0f, attackConfig.detection.forwardOffset);
        postChargeAttackForwardOffset = Mathf.Max(0f, postChargeAttackForwardOffset);
    }
}
