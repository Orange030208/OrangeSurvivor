using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Skeleton_Attack";

    [Header("Attack Timing")]
    [SerializeField, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;
    [SerializeField, Range(0f, 1f)] private float attackFinishNormalizedTime = 0.95f;

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

    public float AttackCommitNormalizedTime => attackCommitNormalizedTime;
    public float AttackFinishNormalizedTime => Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
    public DirectDamageAttackData AttackConfig => attackConfig;

    private void OnValidate()
    {
        attackFinishNormalizedTime = Mathf.Max(attackCommitNormalizedTime, attackFinishNormalizedTime);
        attackConfig.timing.actionId = string.IsNullOrWhiteSpace(attackConfig.timing.actionId) ? ATTACK_ACTION_ID : attackConfig.timing.actionId;
        ValidateAttackTiming(ref attackConfig.timing);
        ValidateForwardCircleDetection(ref attackConfig.detection);
    }

    private static void ValidateAttackTiming(ref AttackTimingData data)
    {
        data.cooldown = Mathf.Max(0f, data.cooldown);
        data.damageMultiplier = Mathf.Max(0f, data.damageMultiplier);
    }

    private static void ValidateForwardCircleDetection(ref ForwardCircleDetectionData data)
    {
        data.fixedRange = Mathf.Max(0f, data.fixedRange);
        data.rangeMultiplier = Mathf.Max(0f, data.rangeMultiplier);
        data.forwardOffset = Mathf.Max(0f, data.forwardOffset);
    }
}
