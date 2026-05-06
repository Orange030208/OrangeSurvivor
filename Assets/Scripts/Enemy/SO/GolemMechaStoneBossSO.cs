using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField, Min(0f)] private float immuneDuration = 1f;
    [SerializeField, Min(0f)] private float glowDuration = 0.8f;
    [SerializeField] private List<PropModifierData> phaseTransitionModifiers = new()
    {
        new PropModifierData(PropType.DamageReduction, PropModifierType.Add, 100f),
    };

    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float meleeCommitNormalizedTime = 0.55f;
    [SerializeField, Range(0f, 1f)] private float meleeFinishNormalizedTime = 0.95f;
    [SerializeField, Range(0f, 1f)] private float shootCommitNormalizedTime = 0.48f;
    [SerializeField, Range(0f, 1f)] private float shootFinishNormalizedTime = 0.9f;

    [Header("Laser")]
    [SerializeField, Min(0f)] private float laserWindupDuration = 0.8f;
    [SerializeField, Min(0f)] private float laserDirectionLockLeadTime = 0.25f;
    [SerializeField, Min(0f)] private float laserDuration = 0.75f;
    [Tooltip("Legacy range value kept for serialized compatibility. Laser length is now resolved by hit tracing.")]
    [SerializeField, Min(0f)] private float laserRange = 9f;
    [SerializeField, Min(0.01f)] private float laserWidth = 1.2f;
    [Tooltip("Only used to draw the beam off-screen when no hittable target is found. This is not a gameplay range limit.")]
    [SerializeField, Min(0.1f)] private float laserNoHitVisualLength = 40f;
    [SerializeField, Min(0.01f)] private float laserDamageInterval = 0.25f;
    [SerializeField, Min(0f)] private float laserDamageMultiplier = 0.65f;
    [SerializeField] private Color laserWindupColor = new(1f, 0.75f, 0.2f, 0.45f);
    [SerializeField] private Color laserActiveColor = new(0.25f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color laserCoreColor = new(1f, 1f, 1f, 0.95f);
    [SerializeField, Min(0.01f)] private float laserWindupVisualWidth = 0.18f;
    [SerializeField, Min(0.01f)] private float laserActiveVisualWidthMultiplier = 1f;
    [SerializeField, Range(0.05f, 1f)] private float laserCoreVisualWidthMultiplier = 0.35f;
    [SerializeField] private int laserSortingOrder = 20;
    [Tooltip("Deprecated. The current laser uses clean LineRenderer colors instead of tiled beam textures.")]
    [SerializeField] private Sprite laserBeamSprite;
    [Tooltip("Deprecated. Kept only to preserve existing serialized data.")]
    [SerializeField, Min(0.01f)] private float laserTextureWorldLength = 2f;
    [Tooltip("Deprecated. Kept only to preserve existing serialized data.")]
    [SerializeField] private float laserScrollSpeed = 2.5f;
    [SerializeField, Min(0f)] private float laserCooldown = 8f;
    [SerializeField, Min(1)] private int laserMinPhase = 2;
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
    [SerializeField] private AudioSfxKey meleeAttackSfxKey = AudioSfxKey.None;
    [SerializeField, Min(0f)] private float meleeCooldown = 1.35f;
    [SerializeField, Min(0f)] private float meleeDamageMultiplier = 1.15f;
    [SerializeField] private AttackRangeSource meleeRangeSource = AttackRangeSource.AttackRangeProp;
    [SerializeField, Min(0f)] private float meleeFixedRange = 1f;
    [SerializeField, Min(0f)] private float meleeRangeMultiplier = 1f;
    [SerializeField, Min(0f)] private float meleeForwardOffset = 0.75f;

    [Header("Shoot Attack")]
    [SerializeField] private AudioSfxKey shootAttackSfxKey = AudioSfxKey.None;
    [SerializeField, Min(0f)] private float shootCooldown = 2f;
    [SerializeField, Min(0f)] private float shootDamageMultiplier = 0.8f;
    [SerializeField] private AttackRangeSource shootRangeSource = AttackRangeSource.DetectionRangeProp;
    [SerializeField, Min(0f)] private float shootFixedRange = 7f;
    [SerializeField, Min(0f)] private float shootRangeMultiplier = 1f;
    [SerializeField] private ProjectileDefinitionSO shootProjectileDefinition;

    public GolemMechaStoneBossAnimationConfig BossAnimConfig => AnimConfig as GolemMechaStoneBossAnimationConfig;
    public float PhaseTwoHealthRatio => phaseTwoHealthRatio;
    public float PhaseThreeHealthRatio => phaseThreeHealthRatio;
    public float ImmuneDuration => immuneDuration;
    public float GlowDuration => glowDuration;
    public IReadOnlyList<PropModifierData> PhaseTransitionModifiers => phaseTransitionModifiers;
    public float MeleeCommitNormalizedTime => meleeCommitNormalizedTime;
    public float MeleeFinishNormalizedTime => Mathf.Max(meleeCommitNormalizedTime, meleeFinishNormalizedTime);
    public float ShootCommitNormalizedTime => shootCommitNormalizedTime;
    public float ShootFinishNormalizedTime => Mathf.Max(shootCommitNormalizedTime, shootFinishNormalizedTime);
    public float LaserWindupDuration => laserWindupDuration;
    public float LaserDirectionLockLeadTime => Mathf.Min(laserDirectionLockLeadTime, laserWindupDuration);
    public float LaserDuration => laserDuration;
    public float LaserRange => laserRange;
    public float LaserWidth => laserWidth;
    public float LaserNoHitVisualLength => Mathf.Max(0.1f, laserNoHitVisualLength);
    public float LaserDamageInterval => laserDamageInterval;
    public float LaserDamageMultiplier => laserDamageMultiplier;
    public Color LaserWindupColor => laserWindupColor;
    public Color LaserActiveColor => laserActiveColor;
    public Color LaserCoreColor => laserCoreColor;
    public float LaserWindupVisualWidth => laserWindupVisualWidth;
    public float LaserActiveVisualWidth => laserWidth * Mathf.Max(0.01f, laserActiveVisualWidthMultiplier);
    public float LaserCoreVisualWidth => LaserActiveVisualWidth * Mathf.Clamp(laserCoreVisualWidthMultiplier, 0.05f, 1f);
    public int LaserSortingOrder => laserSortingOrder;
    public Sprite LaserBeamSprite => laserBeamSprite;
    public float LaserTextureWorldLength => Mathf.Max(0.01f, laserTextureWorldLength);
    public float LaserScrollSpeed => laserScrollSpeed;
    public float LaserCooldown => laserCooldown;
    public int LaserMinPhase => Mathf.Max(1, laserMinPhase);
    public GolemMechaStoneLaserVisual LaserVisualPrefab => laserVisualPrefab;
    public float ShieldDuration => shieldDuration;
    public float ShieldCooldown => shieldCooldown;
    public int ShieldMinPhase => Mathf.Max(1, shieldMinPhase);
    public IReadOnlyList<PropModifierData> ShieldModifiers => shieldModifiers;
    public AudioSfxKey MeleeAttackSfxKey => meleeAttackSfxKey;
    public float MeleeCooldown => Mathf.Max(0f, meleeCooldown);
    public float MeleeDamageMultiplier => Mathf.Max(0f, meleeDamageMultiplier);
    public AttackRangeSource MeleeRangeSource => meleeRangeSource;
    public float MeleeFixedRange => Mathf.Max(0f, meleeFixedRange);
    public float MeleeRangeMultiplier => Mathf.Max(0f, meleeRangeMultiplier);
    public float MeleeForwardOffset => Mathf.Max(0f, meleeForwardOffset);
    public AudioSfxKey ShootAttackSfxKey => shootAttackSfxKey;
    public float ShootCooldown => Mathf.Max(0f, shootCooldown);
    public float ShootDamageMultiplier => Mathf.Max(0f, shootDamageMultiplier);
    public AttackRangeSource ShootRangeSource => shootRangeSource;
    public float ShootFixedRange => Mathf.Max(0f, shootFixedRange);
    public float ShootRangeMultiplier => Mathf.Max(0f, shootRangeMultiplier);
    public ProjectileDefinitionSO ShootProjectileDefinition => shootProjectileDefinition;
    public AttackTimingData MeleeTimingData => new()
    {
        actionId = MELEE_ACTION_ID,
        attackSfxKey = meleeAttackSfxKey,
        cooldown = MeleeCooldown,
        damageMultiplier = MeleeDamageMultiplier,
    };
    public ForwardCircleDetectionData MeleeDetectionData => new()
    {
        rangeSource = meleeRangeSource,
        fixedRange = MeleeFixedRange,
        rangeMultiplier = MeleeRangeMultiplier,
        forwardOffset = MeleeForwardOffset,
    };
    public AttackTimingData ShootTimingData => new()
    {
        actionId = SHOOT_ACTION_ID,
        attackSfxKey = shootAttackSfxKey,
        cooldown = ShootCooldown,
        damageMultiplier = ShootDamageMultiplier,
    };
    public RangeDetectionData ShootDetectionData => new()
    {
        rangeSource = shootRangeSource,
        fixedRange = ShootFixedRange,
        rangeMultiplier = ShootRangeMultiplier,
    };

    private void OnValidate()
    {
        phaseThreeHealthRatio = Mathf.Min(phaseThreeHealthRatio, phaseTwoHealthRatio);
        meleeFinishNormalizedTime = Mathf.Max(meleeCommitNormalizedTime, meleeFinishNormalizedTime);
        shootFinishNormalizedTime = Mathf.Max(shootCommitNormalizedTime, shootFinishNormalizedTime);
        immuneDuration = Mathf.Max(0f, immuneDuration);
        glowDuration = Mathf.Max(0f, glowDuration);
        laserWindupDuration = Mathf.Max(0f, laserWindupDuration);
        laserDirectionLockLeadTime = Mathf.Clamp(laserDirectionLockLeadTime, 0f, laserWindupDuration);
        laserDuration = Mathf.Max(0f, laserDuration);
        laserRange = Mathf.Max(0f, laserRange);
        laserWidth = Mathf.Max(0.01f, laserWidth);
        laserNoHitVisualLength = Mathf.Max(0.1f, laserNoHitVisualLength);
        laserDamageInterval = Mathf.Max(0.01f, laserDamageInterval);
        laserDamageMultiplier = Mathf.Max(0f, laserDamageMultiplier);
        laserWindupVisualWidth = Mathf.Max(0.01f, laserWindupVisualWidth);
        laserActiveVisualWidthMultiplier = Mathf.Max(0.01f, laserActiveVisualWidthMultiplier);
        laserCoreVisualWidthMultiplier = Mathf.Clamp(laserCoreVisualWidthMultiplier, 0.05f, 1f);
        laserTextureWorldLength = Mathf.Max(0.01f, laserTextureWorldLength);
        laserCooldown = Mathf.Max(0f, laserCooldown);
        laserMinPhase = Mathf.Max(1, laserMinPhase);
        shieldDuration = Mathf.Max(0f, shieldDuration);
        shieldCooldown = Mathf.Max(0f, shieldCooldown);
        shieldMinPhase = Mathf.Max(1, shieldMinPhase);
        meleeCooldown = Mathf.Max(0f, meleeCooldown);
        meleeDamageMultiplier = Mathf.Max(0f, meleeDamageMultiplier);
        meleeFixedRange = Mathf.Max(0f, meleeFixedRange);
        meleeRangeMultiplier = Mathf.Max(0f, meleeRangeMultiplier);
        meleeForwardOffset = Mathf.Max(0f, meleeForwardOffset);
        shootCooldown = Mathf.Max(0f, shootCooldown);
        shootDamageMultiplier = Mathf.Max(0f, shootDamageMultiplier);
        shootFixedRange = Mathf.Max(0f, shootFixedRange);
        shootRangeMultiplier = Mathf.Max(0f, shootRangeMultiplier);
    }
}
