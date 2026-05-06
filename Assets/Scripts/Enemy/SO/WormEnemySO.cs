using UnityEngine;

[CreateAssetMenu(fileName = "WormEnemy", menuName = ScriptableObjectMenuPaths.WORM_ENEMY, order = 1)]
public class WormEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Worm_Attack";
    public const string RETREAT_ATTACK_ACTION_ID = "Worm_RetreatAttack";

    [Header("Distance")]
    [Min(0f)] public float retreatTriggerDistance = 4f;
    [Min(0f)] public float retreatCompleteDistance = 7f;
    
    [Header("攻击时机")]
    [Range(0f, 1f)] public float attackCommitNormalizedTime = 0.5f;
    [Range(0f, 1f)] public float attackFinishNormalizedTime = 0.95f;

    [Header("Attacks")]
    public ProjectileAttackData attackConfig = new()
    {
        timing = new AttackTimingData
        {
            actionId = ATTACK_ACTION_ID,
            attackSfxKey = AudioSfxKey.GunshotLight,
            cooldown = 1f,
            damageMultiplier = 1f,
        },
        detection = new RangeDetectionData
        {
            rangeSource = AttackRangeSource.DetectionRangeProp,
            fixedRange = 7f,
            rangeMultiplier = 1f,
        },
    };
    public ProjectileAttackData retreatAttackConfig = new()
    {
        timing = new AttackTimingData
        {
            actionId = RETREAT_ATTACK_ACTION_ID,
            attackSfxKey = AudioSfxKey.GunshotLight,
            cooldown = 1f,
            damageMultiplier = 1f,
        },
        detection = new RangeDetectionData
        {
            rangeSource = AttackRangeSource.DetectionRangeProp,
            fixedRange = 7f,
            rangeMultiplier = 1f,
        },
    };

    [Header("Movement")]
    public RetreatMoveData retreatMovement = new()
    {
        safeDistance = 7f,
        retreatStepDistance = 3f,
    };

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        retreatCompleteDistance = Mathf.Max(retreatTriggerDistance, retreatCompleteDistance);
        attackConfig.timing.actionId = string.IsNullOrWhiteSpace(attackConfig.timing.actionId) ? ATTACK_ACTION_ID : attackConfig.timing.actionId;
        retreatAttackConfig.timing.actionId = string.IsNullOrWhiteSpace(retreatAttackConfig.timing.actionId) ? RETREAT_ATTACK_ACTION_ID : retreatAttackConfig.timing.actionId;
        ValidateAttackConfig(ref attackConfig);
        ValidateAttackConfig(ref retreatAttackConfig);
        retreatMovement.safeDistance = Mathf.Max(retreatTriggerDistance, retreatMovement.safeDistance);
        ValidateRetreatMoveData(ref retreatMovement);
    }

    private static void ValidateAttackConfig(ref ProjectileAttackData config)
    {
        config.timing.cooldown = Mathf.Max(0f, config.timing.cooldown);
        config.timing.damageMultiplier = Mathf.Max(0f, config.timing.damageMultiplier);
        config.detection.fixedRange = Mathf.Max(0f, config.detection.fixedRange);
        config.detection.rangeMultiplier = Mathf.Max(0f, config.detection.rangeMultiplier);
    }

    private static void ValidateRetreatMoveData(ref RetreatMoveData config)
    {
        config.safeDistance = Mathf.Max(0f, config.safeDistance);
        config.retreatStepDistance = Mathf.Max(0f, config.retreatStepDistance);
    }
}
