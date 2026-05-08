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
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField] private EnemyActionDefinition retreatAttackAction = new();
    [HideInInspector, Range(0f, 1f)] public float attackCommitNormalizedTime = 0.5f;

    [Header("Attacks")]
    [Min(0.01f)] public float attackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO attackProjectileDefinition;
    [Min(0.01f)] public float retreatAttackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO retreatAttackProjectileDefinition;

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
        attackSpeedBenefitRatio = Mathf.Max(0.01f, attackSpeedBenefitRatio);
        retreatAttackSpeedBenefitRatio = Mathf.Max(0.01f, retreatAttackSpeedBenefitRatio);
        retreatMovement.safeDistance = Mathf.Max(retreatTriggerDistance, retreatMovement.safeDistance);
        ValidateRetreatMoveData(ref retreatMovement);
        EnsureActionDefaults();
    }

    public EnemyActionDefinition AttackAction
    {
        get
        {
            EnsureActionDefaults();
            return attackAction;
        }
    }

    public EnemyActionDefinition RetreatAttackAction
    {
        get
        {
            EnsureActionDefaults();
            return retreatAttackAction;
        }
    }

    private static void ValidateRetreatMoveData(ref RetreatMoveData config)
    {
        config.safeDistance = Mathf.Max(0f, config.safeDistance);
        config.retreatStepDistance = Mathf.Max(0f, config.retreatStepDistance);
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        retreatAttackAction ??= new EnemyActionDefinition();
        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
        retreatAttackAction.ConfigureDefaults(RETREAT_ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
    }
}
