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

    private void OnValidate()
    {
        normalAttackCommitNormalizedTime = Mathf.Clamp01(normalAttackCommitNormalizedTime);
        normalAttackSpeedBenefitRatio = PropValueUtility.ClampAttackSpeedBenefitRatio(normalAttackSpeedBenefitRatio);
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
