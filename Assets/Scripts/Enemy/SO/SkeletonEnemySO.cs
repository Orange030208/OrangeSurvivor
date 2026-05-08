using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Skeleton_Attack";

    [Header("Attack Timing")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;

    [Header("Attack")]
    [SerializeField, Min(0.01f)] private float attackSpeedBenefitRatio = 1f;

    public EnemyActionDefinition AttackAction
    {
        get
        {
            EnsureActionDefaults();
            return attackAction;
        }
    }

    public float AttackCommitNormalizedTime => AttackAction.CommitNormalizedTime;
    public float AttackSpeedBenefitRatio => Mathf.Max(0.01f, attackSpeedBenefitRatio);

    private void OnValidate()
    {
        attackCommitNormalizedTime = Mathf.Clamp01(attackCommitNormalizedTime);
        attackSpeedBenefitRatio = Mathf.Max(0.01f, attackSpeedBenefitRatio);
        EnsureActionDefaults();
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
    }
}
