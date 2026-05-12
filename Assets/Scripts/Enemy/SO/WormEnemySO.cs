using UnityEngine;

[CreateAssetMenu(fileName = "WormEnemy", menuName = ScriptableObjectMenuPaths.WORM_ENEMY, order = 1)]
public class WormEnemySO : EnemySO
{
    public const string ATTACK_ACTION_ID = "Worm_Attack";
    public const string RETREAT_ATTACK_ACTION_ID = "Worm_RetreatAttack";

    [Header("距离")]
    [Min(0f)] public float retreatTriggerDistance = 4f;
    [Min(0f)] public float retreatCompleteDistance = 7f;
    
    [Header("攻击时机")]
    [SerializeField] private EnemyActionDefinition attackAction = new();
    [SerializeField] private EnemyActionDefinition retreatAttackAction = new();
    [HideInInspector, Range(0f, 1f)] public float attackCommitNormalizedTime = 0.5f;

    [Header("攻击")]
    [Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] public float attackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO attackProjectileDefinition;
    [Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] public float retreatAttackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO retreatAttackProjectileDefinition;

    [Header("移动")]
    public RetreatMoveData retreatMovement = new()
    {
        safeDistanceRatio = 1f,
        retreatStepDistanceRatio = 0.42857143f,
    };

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        retreatCompleteDistance = Mathf.Max(retreatTriggerDistance, retreatCompleteDistance);
        attackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(attackSpeedBenefitRatio);
        retreatAttackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(retreatAttackSpeedBenefitRatio);
        ValidateRetreatMoveData(ref retreatMovement);
        float baseDetectionRange = ResolveBaseDetectionRangeWorldUnits();
        if (baseDetectionRange > Mathf.Epsilon)
        {
            retreatMovement.safeDistanceRatio = Mathf.Max(
                retreatTriggerDistance / baseDetectionRange,
                retreatMovement.safeDistanceRatio);
        }
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
        config.safeDistanceRatio = Mathf.Max(0f, config.safeDistanceRatio);
        config.retreatStepDistanceRatio = Mathf.Max(0f, config.retreatStepDistanceRatio);
    }

    private float ResolveBaseDetectionRangeWorldUnits()
    {
        if (BasePropsAsset == null)
        {
            return 0f;
        }

        var values = BasePropsAsset.Values;
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].propType == PropType.DetectionRange)
            {
                return PropValueUtility.DistancePointsToWorldUnits(values[i].value);
            }
        }

        return 0f;
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
