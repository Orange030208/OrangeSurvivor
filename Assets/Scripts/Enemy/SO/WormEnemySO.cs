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
    public EnemyAttackConfig attackConfig = new()
    {
        actionId = ATTACK_ACTION_ID,
        attackSfxKey = AudioSfxKey.GunshotLight,
        cooldown = 1f,
        damageMultiplier = 1f,
        rangeSource = AttackRangeSource.DetectionRangeProp,
        fixedRange = 7f,
        rangeMultiplier = 1f,
    };
    public EnemyAttackConfig retreatAttackConfig = new()
    {
        actionId = RETREAT_ATTACK_ACTION_ID,
        attackSfxKey = AudioSfxKey.GunshotLight,
        cooldown = 1f,
        damageMultiplier = 1f,
        rangeSource = AttackRangeSource.DetectionRangeProp,
        fixedRange = 7f,
        rangeMultiplier = 1f,
    };

    [Header("Movement")]
    public EnemyMovementConfig approachMovement = new()
    {
        pattern = EnemyMovementPattern.DirectChase,
    };
    public EnemyMovementConfig retreatMovement = new()
    {
        pattern = EnemyMovementPattern.Retreat,
        safeDistance = 7f,
        retreatStepDistance = 3f,
    };

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        retreatCompleteDistance = Mathf.Max(retreatTriggerDistance, retreatCompleteDistance);
        attackConfig.actionId = string.IsNullOrWhiteSpace(attackConfig.actionId) ? ATTACK_ACTION_ID : attackConfig.actionId;
        retreatAttackConfig.actionId = string.IsNullOrWhiteSpace(retreatAttackConfig.actionId) ? RETREAT_ATTACK_ACTION_ID : retreatAttackConfig.actionId;
        ValidateAttackConfig(ref attackConfig);
        ValidateAttackConfig(ref retreatAttackConfig);
        ValidateMovementConfig(ref approachMovement);
        retreatMovement.safeDistance = Mathf.Max(retreatTriggerDistance, retreatMovement.safeDistance);
        ValidateMovementConfig(ref retreatMovement);
    }

    private static void ValidateAttackConfig(ref EnemyAttackConfig config)
    {
        config.cooldown = Mathf.Max(0f, config.cooldown);
        config.damageMultiplier = Mathf.Max(0f, config.damageMultiplier);
        config.fixedRange = Mathf.Max(0f, config.fixedRange);
        config.rangeMultiplier = Mathf.Max(0f, config.rangeMultiplier);
        config.forwardOffset = Mathf.Max(0f, config.forwardOffset);
    }

    private static void ValidateMovementConfig(ref EnemyMovementConfig config)
    {
        config.circleSpeedRatio = Mathf.Max(0f, config.circleSpeedRatio);
        config.idealRangeRatio = Mathf.Max(0f, config.idealRangeRatio);
        config.safeDistance = Mathf.Max(0f, config.safeDistance);
        config.retreatStepDistance = Mathf.Max(0f, config.retreatStepDistance);
    }
}
