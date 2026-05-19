using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SkeletonEnemy", menuName = ScriptableObjectMenuPaths.SKELETON_ENEMY, order = 2)]
public class SkeletonEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Skeleton_Attack";
    private const float DEFAULT_ATTACK_MOVE_FINAL_MULTIPLIER = -100f;

    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float attackCommitNormalizedTime = 0.5f;

    [Header("攻击")]
    [SerializeField, Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] private float attackSpeedBenefitRatio = 1f;
    [SerializeField] private List<PropModifierData> attackStateMoveModifiers = new()
    {
        new(PropType.MoveSpeed, PropModifierType.FinalMultiplier, DEFAULT_ATTACK_MOVE_FINAL_MULTIPLIER)
    };

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
    public IReadOnlyList<PropModifierData> AttackStateMoveModifiers
    {
        get
        {
            EnsureAttackStateMoveModifierDefaults();
            return attackStateMoveModifiers;
        }
    }

    protected virtual string DefaultAttackActionId => ATTACK_ACTION_ID;
    protected virtual string DefaultAttackAnimationStateName => AnimConfig != null ? AnimConfig.Attack : "Attack";

    protected virtual void OnValidate()
    {
        attackCommitNormalizedTime = Mathf.Clamp01(attackCommitNormalizedTime);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        EnsureActionDefaults();
        EnsureAttackStateMoveModifierDefaults();
    }

    protected void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        attackAction.ConfigureDefaults(DefaultAttackActionId, DefaultAttackAnimationStateName, attackCommitNormalizedTime);
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
}
