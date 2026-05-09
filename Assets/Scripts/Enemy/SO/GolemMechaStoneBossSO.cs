using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GolemMechaStoneBoss", menuName = ScriptableObjectMenuPaths.GOLEM_MECHA_STONE_BOSS, order = 4)]
public sealed class GolemMechaStoneBossSO : EnemySO
{
    public const string MELEE_ACTION_ID = "GolemMechaStoneBoss_Melee";
    public const string SHOOT_ACTION_ID = "GolemMechaStoneBoss_Shoot";
    public const string LASER_ACTION_ID = "GolemMechaStoneBoss_Laser";
    public const string SHIELD_ACTION_ID = "GolemMechaStoneBoss_Shield";

    [Header("Phase")]
    [SerializeField, Range(0f, 1f)] private float phaseTwoHealthRatio = 0.7f;
    [SerializeField, Range(0f, 1f)] private float phaseThreeHealthRatio = 0.35f;
    [SerializeField] private List<PropModifierData> phaseTransitionModifiers = new()
    {
        new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 100f),
    };

    [Header("Attack Timing")]
    [SerializeField] private EnemyActionDefinition meleeAction = new();
    [SerializeField] private EnemyActionDefinition shootAction = new();
    [SerializeField] private EnemyActionDefinition laserAction = new();
    [SerializeField] private EnemyActionDefinition shieldAction = new();
    [SerializeField] private EnemyActionDefinition phaseChangeAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float meleeCommitNormalizedTime = 0.55f;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float shootCommitNormalizedTime = 0.48f;

    [Header("Laser")]
    [Tooltip("激光预瞄锁定目标方向的动画归一化时间；到达该时间后，开火前不再继续追踪玩家位置。")]
    [SerializeField, Range(0f, 1f)] private float laserAimLockNormalizedTime = 0.65f;
    [SerializeField, Range(0f, 1f)] private float laserFireStartNormalizedTime = 0.35f;
    [SerializeField, Min(0f)] private float laserDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float laserWidth = 1.2f;
    [Tooltip("Laser gameplay and visual length. The beam pierces targets within this length instead of stopping at the first hit.")]
    [SerializeField, Min(0.1f)] private float laserLength = 40f;
    [SerializeField, Min(0.01f)] private float laserDamageInterval = 0.25f;
    [SerializeField, Min(0f)] private float laserDamageMultiplier = 0.65f;
    [Tooltip("Maximum active laser turn speed in degrees per second. Use 0 to keep the initial fire direction locked.")]
    [SerializeField, Min(0f)] private float laserTurnSpeedDegrees = 18f;
    [SerializeField, Min(0f)] private float laserCooldown = 8f;
    [SerializeField, Min(1)] private int laserMinPhase = 2;
    [SerializeField, Min(0f)] private float laserRangeMultiplier = 1f;
    [SerializeField] private GolemMechaStoneLaserVisual laserVisualPrefab;

    [Header("Shield")]
    [SerializeField, Min(0f)] private float shieldDuration = 3f;
    [SerializeField, Min(0f)] private float shieldCooldown = 12f;
    [SerializeField, Min(1)] private int shieldMinPhase = 3;
    [SerializeField] private List<PropModifierData> shieldModifiers = new()
    {
        new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 40f),
    };

    [Header("Melee Attack")]
    [SerializeField, Min(0.01f)] private float meleeAttackSpeedBenefitRatio = 0.75f;
    [SerializeField, Min(0f)] private float meleeRangeMultiplier = 1f;
    [Tooltip("近战攻击提交时在实际攻击区域中心生成的挥出特效预制体；空挥也会生成。")]
    [SerializeField] private GameObject meleeHitVfxPrefab;

    [Header("Shoot Attack")]
    [SerializeField, Min(0.01f)] private float shootAttackSpeedBenefitRatio = 0.5f;
    [SerializeField, Min(0f)] private float shootRangeMultiplier = 1f;
    [SerializeField] private ProjectileDefinitionSO shootProjectileDefinition;

    public GolemMechaStoneBossAnimationConfig BossAnimConfig => AnimConfig as GolemMechaStoneBossAnimationConfig;
    public float PhaseTwoHealthRatio => phaseTwoHealthRatio;
    public float PhaseThreeHealthRatio => phaseThreeHealthRatio;
    public IReadOnlyList<PropModifierData> PhaseTransitionModifiers => phaseTransitionModifiers;
    public EnemyActionDefinition MeleeAction
    {
        get
        {
            EnsureActionDefaults();
            return meleeAction;
        }
    }

    public EnemyActionDefinition ShootAction
    {
        get
        {
            EnsureActionDefaults();
            return shootAction;
        }
    }

    public EnemyActionDefinition LaserAction
    {
        get
        {
            EnsureActionDefaults();
            return laserAction;
        }
    }

    public EnemyActionDefinition ShieldAction
    {
        get
        {
            EnsureActionDefaults();
            return shieldAction;
        }
    }

    public EnemyActionDefinition PhaseChangeAction
    {
        get
        {
            EnsureActionDefaults();
            return phaseChangeAction;
        }
    }

    public float MeleeCommitNormalizedTime => MeleeAction.CommitNormalizedTime;
    public float ShootCommitNormalizedTime => ShootAction.CommitNormalizedTime;
    public float LaserAimLockNormalizedTime => Mathf.Clamp01(laserAimLockNormalizedTime);
    public float LaserFireStartNormalizedTime => laserFireStartNormalizedTime;
    public float LaserDuration => laserDuration;
    public float LaserWidth => laserWidth;
    public float LaserLength => Mathf.Max(0.1f, laserLength);
    public float LaserDamageInterval => laserDamageInterval;
    public float LaserDamageMultiplier => laserDamageMultiplier;
    public float LaserTurnSpeedDegrees => Mathf.Max(0f, laserTurnSpeedDegrees);
    public float LaserCooldown => laserCooldown;
    public int LaserMinPhase => Mathf.Max(1, laserMinPhase);
    public float LaserRangeMultiplier => Mathf.Max(0f, laserRangeMultiplier);
    public GolemMechaStoneLaserVisual LaserVisualPrefab => laserVisualPrefab;
    public float ShieldDuration => shieldDuration;
    public float ShieldCooldown => shieldCooldown;
    public int ShieldMinPhase => Mathf.Max(1, shieldMinPhase);
    public IReadOnlyList<PropModifierData> ShieldModifiers => shieldModifiers;
    public float MeleeAttackSpeedBenefitRatio => Mathf.Max(0.01f, meleeAttackSpeedBenefitRatio);
    public float MeleeRangeMultiplier => Mathf.Max(0f, meleeRangeMultiplier);
    public GameObject MeleeHitVfxPrefab => meleeHitVfxPrefab;
    public float ShootAttackSpeedBenefitRatio => Mathf.Max(0.01f, shootAttackSpeedBenefitRatio);
    public float ShootRangeMultiplier => Mathf.Max(0f, shootRangeMultiplier);
    public ProjectileDefinitionSO ShootProjectileDefinition => shootProjectileDefinition;
    private void OnValidate()
    {
        phaseThreeHealthRatio = Mathf.Min(phaseThreeHealthRatio, phaseTwoHealthRatio);
        laserAimLockNormalizedTime = Mathf.Clamp(laserAimLockNormalizedTime, 0f, laserFireStartNormalizedTime);
        laserFireStartNormalizedTime = Mathf.Clamp01(laserFireStartNormalizedTime);
        laserDuration = Mathf.Max(0f, laserDuration);
        laserWidth = Mathf.Max(0.01f, laserWidth);
        laserLength = Mathf.Max(0.1f, laserLength);
        laserDamageInterval = Mathf.Max(0.01f, laserDamageInterval);
        laserDamageMultiplier = Mathf.Max(0f, laserDamageMultiplier);
        laserTurnSpeedDegrees = Mathf.Max(0f, laserTurnSpeedDegrees);
        laserCooldown = Mathf.Max(0f, laserCooldown);
        laserMinPhase = Mathf.Max(1, laserMinPhase);
        laserRangeMultiplier = Mathf.Max(0f, laserRangeMultiplier);
        shieldDuration = Mathf.Max(0f, shieldDuration);
        shieldCooldown = Mathf.Max(0f, shieldCooldown);
        shieldMinPhase = Mathf.Max(1, shieldMinPhase);
        meleeAttackSpeedBenefitRatio = Mathf.Max(0.01f, meleeAttackSpeedBenefitRatio);
        meleeRangeMultiplier = Mathf.Max(0f, meleeRangeMultiplier);
        shootAttackSpeedBenefitRatio = Mathf.Max(0.01f, shootAttackSpeedBenefitRatio);
        shootRangeMultiplier = Mathf.Max(0f, shootRangeMultiplier);
        EnsureActionDefaults();
    }

    private void EnsureActionDefaults()
    {
        meleeAction ??= new EnemyActionDefinition();
        shootAction ??= new EnemyActionDefinition();
        laserAction ??= new EnemyActionDefinition();
        shieldAction ??= new EnemyActionDefinition();
        phaseChangeAction ??= new EnemyActionDefinition();

        GolemMechaStoneBossAnimationConfig animConfig = BossAnimConfig;
        meleeAction.ConfigureDefaults(
            MELEE_ACTION_ID,
            animConfig != null ? animConfig.Melee : "Melee",
            meleeCommitNormalizedTime);
        shootAction.ConfigureDefaults(
            SHOOT_ACTION_ID,
            animConfig != null ? animConfig.Shoot : "Shoot",
            shootCommitNormalizedTime);
        laserAction.ConfigureDefaults(
            LASER_ACTION_ID,
            animConfig != null ? animConfig.LaserCast : "LaserCast",
            laserFireStartNormalizedTime,
            EnemyActionCompletionMode.Manual);
        shieldAction.ConfigureDefaults(
            SHIELD_ACTION_ID,
            animConfig != null ? animConfig.ShieldCast : "ShieldCast",
            0f,
            EnemyActionCompletionMode.Duration,
            false,
            shieldDuration);
        phaseChangeAction.ConfigureDefaults(
            "GolemMechaStoneBoss_PhaseChange",
            animConfig != null ? animConfig.Immune : "Immune",
            0f,
            EnemyActionCompletionMode.AnimationNormalizedTime,
            false);
    }
}
