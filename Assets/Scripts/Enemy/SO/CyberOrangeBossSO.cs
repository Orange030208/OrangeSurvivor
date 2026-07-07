using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CyberOrangeBoss", menuName = ScriptableObjectMenuPaths.CYBER_ORANGE_BOSS, order = 4)]
public sealed class CyberOrangeBossSO : EnemySO
{
    public const string ATTACK_ACTION_ID = "CyberOrangeBoss_Attack";
    public const string CHARGE_ACTION_ID = "CyberOrangeBoss_Charge";
    public const string BARRAGE_SKILL_ID = "CyberOrangeBoss_Barrage";
    private const float DEFAULT_ATTACK_MOVE_FINAL_MULTIPLIER = -100f;
    private const string DEFAULT_ATTACK_ANIMATION_STATE = "Attack";
    private const string DEFAULT_CHARGE_ANIMATION_STATE = "Charge";

    [Header("动作时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField] private EnemyActionDefinition chargeAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;

    [Header("普通近战")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float attackSpeedBenefitRatio = 0.85f;
    [SerializeField, Min(0f)] private float attackRangeMultiplier = 1.2f;
    [SerializeField] private List<PropModifierData> attackStateMoveModifiers = new()
    {
        new(PropType.MoveSpeed, PropModifierType.FinalMultiplier, DEFAULT_ATTACK_MOVE_FINAL_MULTIPLIER)
    };
    [SerializeField] private DirectDamageHitShape attackHitShape = DirectDamageHitShape.FacingSemicircle;
    [SerializeField] private bool enableAttackScreenShake = true;
    [SerializeField] private ScreenShakeSettings attackScreenShake = ScreenShakeSettings.CreateBossMeleeDefault();
    [SerializeField, Min(0f)] private float attackScreenShakeScale = 1f;

    [Header("冲撞技能")]
    [SerializeField, Min(0f)] private float chargeCooldown = 6f;
    [SerializeField, Min(0f)] private float chargeWindupDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float chargeDuration = 1.15f;
    [SerializeField, Min(0f)] private float chargeDamageRadius = 1.45f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1.6f;
    [SerializeField] private List<PropModifierData> chargeModifiers = new()
    {
        new(PropType.MoveSpeed, PropModifierType.FinalMultiplier, 160f)
    };
    [SerializeField] private bool enableChargeScreenShake = true;
    [SerializeField] private ScreenShakeSettings chargeScreenShake = ScreenShakeSettings.CreateBossMeleeDefault();
    [SerializeField, Min(0f)] private float chargeScreenShakeScale = 1.2f;

    [Header("远程压制技能")]
    [SerializeField, Min(0f)] private float barrageCooldown = 7.5f;
    [SerializeField, Min(0f)] private float barrageWindupDuration = 0.4f;
    [SerializeField, Min(1)] private int barrageShotCount = 3;
    [SerializeField, Min(0f)] private float barrageShotInterval = 0.16f;
    [SerializeField, Min(0f)] private float barrageSpreadAngle = 18f;
    [SerializeField, Min(0f)] private float barrageRangeMultiplier = 1.45f;
    [SerializeField, Min(0f)] private float barrageDamageMultiplier = 0.7f;
    [SerializeField] private ProjectileDefinitionSO barrageProjectileDefinition;
    [SerializeField] private AudioSfxKey barrageSfxKey = AudioSfxKey.GenericProjectileLaunch;

    [Header("血线狂暴")]
    [SerializeField, Range(0f, 1f)] private float enrageHealthRatio = 0.45f;
    [SerializeField] private List<PropModifierData> enrageModifiers = new()
    {
        new(PropType.MoveSpeed, PropModifierType.FinalMultiplier, 20f),
        new(PropType.AttackSpeed, PropModifierType.FinalMultiplier, 20f),
        new(PropType.Attack, PropModifierType.FinalMultiplier, 15f)
    };
    [SerializeField] private AudioSfxKey enrageSfxKey = AudioSfxKey.None;

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

    public float AttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
    public float AttackRangeMultiplier => Mathf.Max(0f, attackRangeMultiplier);
    public IReadOnlyList<PropModifierData> AttackStateMoveModifiers
    {
        get
        {
            EnsureAttackStateMoveModifierDefaults();
            return attackStateMoveModifiers;
        }
    }

    public DirectDamageHitShape AttackHitShape => attackHitShape;
    public ScreenShakeSettings AttackScreenShake
    {
        get
        {
            EnsureScreenShakeDefaults();
            return enableAttackScreenShake ? attackScreenShake : null;
        }
    }

    public float AttackScreenShakeScale => Mathf.Max(0f, attackScreenShakeScale);
    public float ChargeCooldown => Mathf.Max(0f, chargeCooldown);
    public float ChargeWindupDuration => Mathf.Max(0f, chargeWindupDuration);
    public float ChargeDuration => Mathf.Max(0.01f, chargeDuration);
    public float ChargeDamageRadius => Mathf.Max(0f, chargeDamageRadius);
    public float ChargeDamageMultiplier => Mathf.Max(0f, chargeDamageMultiplier);
    public IReadOnlyList<PropModifierData> ChargeModifiers => chargeModifiers;
    public ScreenShakeSettings ChargeScreenShake
    {
        get
        {
            EnsureScreenShakeDefaults();
            return enableChargeScreenShake ? chargeScreenShake : null;
        }
    }

    public float ChargeScreenShakeScale => Mathf.Max(0f, chargeScreenShakeScale);
    public float BarrageCooldown => Mathf.Max(0f, barrageCooldown);
    public float BarrageWindupDuration => Mathf.Max(0f, barrageWindupDuration);
    public int BarrageShotCount => Mathf.Max(1, barrageShotCount);
    public float BarrageShotInterval => Mathf.Max(0f, barrageShotInterval);
    public float BarrageSpreadAngle => Mathf.Max(0f, barrageSpreadAngle);
    public float BarrageRangeMultiplier => Mathf.Max(0f, barrageRangeMultiplier);
    public float BarrageDamageMultiplier => Mathf.Max(0f, barrageDamageMultiplier);
    public ProjectileDefinitionSO BarrageProjectileDefinition => barrageProjectileDefinition;
    public AudioSfxKey BarrageSfxKey => barrageSfxKey;
    public float EnrageHealthRatio => Mathf.Clamp01(enrageHealthRatio);
    public IReadOnlyList<PropModifierData> EnrageModifiers => enrageModifiers;
    public AudioSfxKey EnrageSfxKey => enrageSfxKey;

    private void OnValidate()
    {
        role = EnemyRole.Boss;
        attackCommitNormalizedTime = Mathf.Clamp01(attackCommitNormalizedTime);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        attackRangeMultiplier = Mathf.Max(0f, attackRangeMultiplier);
        chargeCooldown = Mathf.Max(0f, chargeCooldown);
        chargeWindupDuration = Mathf.Max(0f, chargeWindupDuration);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        chargeDamageRadius = Mathf.Max(0f, chargeDamageRadius);
        chargeDamageMultiplier = Mathf.Max(0f, chargeDamageMultiplier);
        barrageCooldown = Mathf.Max(0f, barrageCooldown);
        barrageWindupDuration = Mathf.Max(0f, barrageWindupDuration);
        barrageShotCount = Mathf.Max(1, barrageShotCount);
        barrageShotInterval = Mathf.Max(0f, barrageShotInterval);
        barrageSpreadAngle = Mathf.Max(0f, barrageSpreadAngle);
        barrageRangeMultiplier = Mathf.Max(0f, barrageRangeMultiplier);
        barrageDamageMultiplier = Mathf.Max(0f, barrageDamageMultiplier);
        enrageHealthRatio = Mathf.Clamp01(enrageHealthRatio);
        EnsureActionDefaults();
        EnsureAttackStateMoveModifierDefaults();
        EnsureScreenShakeDefaults();
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        chargeAction ??= new EnemyActionDefinition();

        string attackStateName = AnimConfig != null ? AnimConfig.Attack : DEFAULT_ATTACK_ANIMATION_STATE;
        string chargeStateName = AnimConfig != null ? AnimConfig.Charge : DEFAULT_CHARGE_ANIMATION_STATE;
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
        chargeAction.ConfigureDefaults(
            CHARGE_ACTION_ID,
            chargeStateName,
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            chargeWindupDuration);
    }

    private void EnsureAttackStateMoveModifierDefaults()
    {
        attackStateMoveModifiers ??= new List<PropModifierData>();
        if (attackStateMoveModifiers.Count > 0)
        {
            return;
        }

        attackStateMoveModifiers.Add(new PropModifierData(
            PropType.MoveSpeed,
            PropModifierType.FinalMultiplier,
            DEFAULT_ATTACK_MOVE_FINAL_MULTIPLIER));
    }

    private void EnsureScreenShakeDefaults()
    {
        attackScreenShake ??= ScreenShakeSettings.CreateBossMeleeDefault();
        attackScreenShake.OnValidate();
        chargeScreenShake ??= ScreenShakeSettings.CreateBossMeleeDefault();
        chargeScreenShake.OnValidate();
    }
}
