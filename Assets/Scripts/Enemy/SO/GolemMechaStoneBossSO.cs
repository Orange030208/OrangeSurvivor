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

    [Header("阶段")]
    [SerializeField, Range(0f, 1f)] private float phaseTwoHealthRatio = 0.7f;
    [SerializeField, Range(0f, 1f)] private float phaseThreeHealthRatio = 0.35f;
    [SerializeField] private List<PropModifierData> phaseTransitionModifiers = new()
    {
        new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 100f),
    };

    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition meleeAction = new();
    [SerializeField] private EnemyActionDefinition shootAction = new();
    [SerializeField] private EnemyActionDefinition laserAction = new();
    [SerializeField] private EnemyActionDefinition shieldAction = new();
    [SerializeField] private EnemyActionDefinition phaseChangeAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float meleeCommitNormalizedTime = 0.55f;
    [SerializeField, HideInInspector, Range(0f, 1f)] private float shootCommitNormalizedTime = 0.48f;

    [Header("激光")]
    [Tooltip("激光预瞄锁定目标方向的动画归一化时间；到达该时间后，开火前不再继续追踪玩家位置。")]
    [SerializeField, Range(0f, 1f)] private float laserAimLockNormalizedTime = 0.65f;
    [SerializeField, Range(0f, 1f)] private float laserFireStartNormalizedTime = 0.35f;
    [SerializeField, Min(0f)] private float laserDuration = 0.75f;
    [SerializeField, Min(0.01f)] private float laserWidth = 1.2f;
    [Tooltip("激光的玩法判定长度与视觉长度。光束会贯穿该长度内的目标，而不是命中第一个目标后停止。")]
    [SerializeField, Min(0.1f)] private float laserLength = 40f;
    [SerializeField, Min(0.01f)] private float laserDamageInterval = 0.25f;
    [SerializeField, Min(0f)] private float laserDamageMultiplier = 0.65f;
    [Tooltip("激光激活期间每秒最大转向角度。设为 0 时保持初始开火方向锁定。")]
    [SerializeField, Min(0f)] private float laserTurnSpeedDegrees = 18f;
    [SerializeField, Min(0f)] private float laserCooldown = 8f;
    [SerializeField, Min(1)] private int laserMinPhase = 2;
    [SerializeField, Min(0f)] private float laserRangeMultiplier = 1f;
    [SerializeField] private GolemMechaStoneLaserVisual laserVisualPrefab;

    [Header("护盾")]
    [SerializeField, Min(0f)] private float shieldDuration = 3f;
    [SerializeField, Min(0f)] private float shieldCooldown = 12f;
    [SerializeField, Min(1)] private int shieldMinPhase = 3;
    [SerializeField] private List<PropModifierData> shieldModifiers = new()
    {
        new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 40f),
    };

    [Header("近战攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float meleeAttackSpeedBenefitRatio = 0.75f;
    [SerializeField, Min(0f)] private float meleeRangeMultiplier = 1f;
    [Tooltip("近战攻击提交时在实际攻击区域中心生成的挥出特效预制体；空挥也会生成。")]
    [SerializeField] private GameObject meleeHitVfxPrefab;
    [SerializeField] private bool enableMeleeScreenShake = true;
    [SerializeField] private ScreenShakeSettings meleeScreenShake = ScreenShakeSettings.CreateBossMeleeDefault();
    [SerializeField, Min(0f)] private float meleeScreenShakeScale = 1f;

    [Header("射击攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float shootAttackSpeedBenefitRatio = 0.5f;
    [Tooltip("射击的释放入场距离和投射物最大飞行距离倍率。近战使用近战倍率，激光使用激光长度配置。")]
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
    public float MeleeAttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(meleeAttackSpeedBenefitRatio);
    public float MeleeRangeMultiplier => Mathf.Max(0f, meleeRangeMultiplier);
    public GameObject MeleeHitVfxPrefab => meleeHitVfxPrefab;
    public ScreenShakeSettings MeleeScreenShake
    {
        get
        {
            EnsureMeleeScreenShakeDefaults();
            return enableMeleeScreenShake ? meleeScreenShake : null;
        }
    }

    public float MeleeScreenShakeScale => Mathf.Max(0f, meleeScreenShakeScale);
    public float ShootAttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(shootAttackSpeedBenefitRatio);
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
        meleeAttackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(meleeAttackSpeedBenefitRatio);
        meleeRangeMultiplier = Mathf.Max(0f, meleeRangeMultiplier);
        EnsureMeleeScreenShakeDefaults();
        meleeScreenShakeScale = Mathf.Max(0f, meleeScreenShakeScale);
        shootAttackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(shootAttackSpeedBenefitRatio);
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

    private void EnsureMeleeScreenShakeDefaults()
    {
        meleeScreenShake ??= ScreenShakeSettings.CreateBossMeleeDefault();
        meleeScreenShake.OnValidate();
    }
}
