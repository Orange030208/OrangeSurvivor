using UnityEngine;

[CreateAssetMenu(fileName = "FlyForestEnemy", menuName = ScriptableObjectMenuPaths.FLY_FOREST, order = 0)]
public class FlyForestEnemySO : EnemySO
{
    public const string NORMAL_ATTACK_ACTION_ID = "FlyForest_NormalAttack";

    [Header("攻击")]
    [SerializeField] private EnemyActionDefinition normalAttackAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float normalAttackCommitNormalizedTime = 0.578f;
    [Min(PropValueUtility.MIN_ATTACK_SPEED_BENEFIT_RATIO)] public float normalAttackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO normalAttackProjectileDefinition;

    [Header("移动")]
    public CircleKiteMoveData normalMovement = new()
    {
        circleSpeedRatio = 0.5f,
        idealRangeRatio = 0.95f,
    };

    private void OnValidate()
    {
        normalAttackCommitNormalizedTime = Mathf.Clamp01(normalAttackCommitNormalizedTime);
        normalAttackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(normalAttackSpeedBenefitRatio);
        normalMovement.circleSpeedRatio = Mathf.Max(0f, normalMovement.circleSpeedRatio);
        normalMovement.idealRangeRatio = Mathf.Max(0f, normalMovement.idealRangeRatio);
        EnsureActionDefaults();
    }

    public EnemyActionDefinition NormalAttackAction
    {
        get
        {
            EnsureActionDefaults();
            return normalAttackAction;
        }
    }

    public float NormalAttackCommitNormalizedTime => NormalAttackAction.CommitNormalizedTime;

    private void EnsureActionDefaults()
    {
        normalAttackAction ??= new EnemyActionDefinition();
        string attackStateName = AnimConfig != null ? AnimConfig.Attack : "Attack";
        normalAttackAction.ConfigureDefaults(NORMAL_ATTACK_ACTION_ID, attackStateName, normalAttackCommitNormalizedTime);
    }
}
