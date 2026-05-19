using UnityEngine;

[CreateAssetMenu(fileName = "WormEnemy", menuName = ScriptableObjectMenuPaths.WORM_ENEMY, order = 1)]
public class WormEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Worm_Attack";

    [Header("距离")]
    [SerializeField, Min(0f)] private float retreatTriggerRangeRatio = 0.8f;
    [SerializeField, Min(0f)] private float retreatCompleteRangeRatio = 1.4f;
    
    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [HideInInspector, Range(0f, 1f)] public float attackCommitNormalizedTime = 0.5f;

    [Header("攻击")]
    [Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] public float attackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO attackProjectileDefinition;

    [Header("移动")]
    public RetreatMoveData retreatMovement = new()
    {
        safeDistanceRatio = 1f,
        retreatStepDistanceRatio = 0.42857143f,
    };

    private void OnValidate()
    {
        retreatTriggerRangeRatio = Mathf.Max(0f, retreatTriggerRangeRatio);
        retreatCompleteRangeRatio = Mathf.Max(retreatTriggerRangeRatio, retreatCompleteRangeRatio);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        ValidateRetreatMoveData(ref retreatMovement);
        retreatMovement.safeDistanceRatio = Mathf.Max(retreatCompleteRangeRatio, retreatMovement.safeDistanceRatio);
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

    public float RetreatTriggerRangeRatio => Mathf.Max(0f, retreatTriggerRangeRatio);
    public float RetreatCompleteRangeRatio => Mathf.Max(RetreatTriggerRangeRatio, retreatCompleteRangeRatio);

    private static void ValidateRetreatMoveData(ref RetreatMoveData config)
    {
        config.safeDistanceRatio = Mathf.Max(0f, config.safeDistanceRatio);
        config.retreatStepDistanceRatio = Mathf.Max(0f, config.retreatStepDistanceRatio);
    }

    private void EnsureActionDefaults()
    {
        attackAction ??= new EnemyActionDefinition();
        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        attackAction.ConfigureDefaults(ATTACK_ACTION_ID, attackStateName, attackCommitNormalizedTime);
    }
}
