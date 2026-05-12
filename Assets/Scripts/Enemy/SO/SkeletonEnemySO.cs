using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Skeleton_Attack";

    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;

    [Header("攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float attackSpeedBenefitRatio = 1f;

    public EnemyActionDefinition AttackAction
    {
        get
        {
            EnsureActionDefaults();
            return attackAction;
        }
    }

    public float AttackCommitNormalizedTime => AttackAction.CommitNormalizedTime;
    public float AttackSpeedBenefitRatio => PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
    protected virtual string DefaultAttackActionId => ATTACK_ACTION_ID;
    protected virtual string DefaultAttackAnimationStateName => AnimConfig != null ? AnimConfig.Attack : "Attack";

    protected virtual void OnValidate()
    {
        attackCommitNormalizedTime = Mathf.Clamp01(attackCommitNormalizedTime);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        EnsureActionDefaults();
    }

    protected void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        attackAction.ConfigureDefaults(DefaultAttackActionId, DefaultAttackAnimationStateName, attackCommitNormalizedTime);
    }
}
