using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlyForestEnemy", menuName = ScriptableObjectMenuPaths.FLY_FOREST, order = 0)]
public class FlyForestEnemySO : EnemySO
{
    public const string NORMAL_ATTACK_ACTION_ID = "FlyForest_NormalAttack";

    [Header("状态阈值")]
    [Range(0f, 100f)] 
    public float lowHpPercent = 30f;
    
    public List<PropModifierData> fastBurstModifierData = new List<PropModifierData>();

    [Header("攻击")]
    [SerializeField] private EnemyActionDefinition normalAttackAction = new();
    [SerializeField, HideInInspector, Range(0f, 1f)] private float normalAttackCommitNormalizedTime = 0.578f;
    [Min(0.01f)] public float normalAttackSpeedBenefitRatio = 1f;
    public ProjectileDefinitionSO normalAttackProjectileDefinition;

    [Header("移动")]
    public CircleKiteMoveData normalMovement = new()
    {
        circleSpeedRatio = 0.5f,
        idealRangeRatio = 0.95f,
    };
    public RetreatMoveData retreatMovement = new()
    {
        safeDistance = 8f,
        retreatStepDistance = 3f,
    };

    private void OnValidate()
    {
        lowHpPercent = Mathf.Clamp(lowHpPercent, 0f, 100f);
        normalAttackCommitNormalizedTime = Mathf.Clamp01(normalAttackCommitNormalizedTime);
        normalAttackSpeedBenefitRatio = Mathf.Max(0.01f, normalAttackSpeedBenefitRatio);
        normalMovement.circleSpeedRatio = Mathf.Max(0f, normalMovement.circleSpeedRatio);
        normalMovement.idealRangeRatio = Mathf.Max(0f, normalMovement.idealRangeRatio);
        retreatMovement.safeDistance = Mathf.Max(0f, retreatMovement.safeDistance);
        retreatMovement.retreatStepDistance = Mathf.Max(0f, retreatMovement.retreatStepDistance);
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
