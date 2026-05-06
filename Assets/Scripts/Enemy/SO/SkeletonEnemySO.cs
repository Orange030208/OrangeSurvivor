using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Skeleton_Attack";

    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;
    [SerializeField, Range(0f, 1f)] private float attackFinishNormalizedTime = 0.95f;

    [Header("Attack")]
    [SerializeField] private EnemyAttackConfig attackConfig = new()
    {
        actionId = ATTACK_ACTION_ID,
        attackSfxKey = AudioSfxKey.Slap,
        cooldown = 1f,
        damageMultiplier = 1f,
        rangeSource = AttackRangeSource.AttackRangeProp,
        fixedRange = 1f,
        rangeMultiplier = 1f,
        forwardOffset = 0f,
    };

    [Header("Movement")]
    [SerializeField] private EnemyMovementConfig chaseMovement = new()
    {
        pattern = EnemyMovementPattern.DirectChase,
    };

    public float AttackCommitNormalizedTime => attackCommitNormalizedTime;
    public float AttackFinishNormalizedTime => Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
    public EnemyAttackConfig AttackConfig => attackConfig;
    public EnemyMovementConfig ChaseMovement => chaseMovement;

    private void OnValidate()
    {
        attackFinishNormalizedTime = Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
        attackConfig.actionId = string.IsNullOrWhiteSpace(attackConfig.actionId) ? ATTACK_ACTION_ID : attackConfig.actionId;
        attackConfig.cooldown = Mathf.Max(0f, attackConfig.cooldown);
        attackConfig.damageMultiplier = Mathf.Max(0f, attackConfig.damageMultiplier);
        attackConfig.fixedRange = Mathf.Max(0f, attackConfig.fixedRange);
        attackConfig.rangeMultiplier = Mathf.Max(0f, attackConfig.rangeMultiplier);
        attackConfig.forwardOffset = Mathf.Max(0f, attackConfig.forwardOffset);
        chaseMovement.circleSpeedRatio = Mathf.Max(0f, chaseMovement.circleSpeedRatio);
        chaseMovement.idealRangeRatio = Mathf.Max(0f, chaseMovement.idealRangeRatio);
        chaseMovement.safeDistance = Mathf.Max(0f, chaseMovement.safeDistance);
        chaseMovement.retreatStepDistance = Mathf.Max(0f, chaseMovement.retreatStepDistance);
    }
}
