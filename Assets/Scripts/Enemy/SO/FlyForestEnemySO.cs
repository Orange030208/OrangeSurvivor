using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlyForestEnemy", menuName = ScriptableObjectMenuPaths.FLY_FOREST, order = 0)]
public class FlyForestEnemySO : EnemySO
{
    public const string NORMAL_ATTACK_ACTION_ID = "FlyForest_NormalAttack";

    [Header("State Thresholds")]
    [Range(0f, 100f)] 
    public float lowHpPercent = 30f;
    
    public List<PropModifierData> fastBurstModifierData = new List<PropModifierData>();

    [Header("Attack")]
    public EnemyAttackConfig normalAttackConfig = new()
    {
        actionId = NORMAL_ATTACK_ACTION_ID,
        attackSfxKey = AudioSfxKey.GunshotLight,
        cooldown = 1f,
        damageMultiplier = 1f,
        rangeSource = AttackRangeSource.DetectionRangeProp,
        fixedRange = 7f,
        rangeMultiplier = 1f,
    };

    [Header("Movement")]
    public EnemyMovementConfig normalMovement = new()
    {
        pattern = EnemyMovementPattern.CircleKite,
        circleSpeedRatio = 0.5f,
        idealRangeRatio = 0.95f,
    };
    public EnemyMovementConfig retreatMovement = new()
    {
        pattern = EnemyMovementPattern.Retreat,
        safeDistance = 8f,
        retreatStepDistance = 3f,
    };

    private void OnValidate()
    {
        lowHpPercent = Mathf.Clamp(lowHpPercent, 0f, 100f);
        normalAttackConfig.actionId = string.IsNullOrWhiteSpace(normalAttackConfig.actionId) ? NORMAL_ATTACK_ACTION_ID : normalAttackConfig.actionId;
        normalAttackConfig.cooldown = Mathf.Max(0f, normalAttackConfig.cooldown);
        normalAttackConfig.damageMultiplier = Mathf.Max(0f, normalAttackConfig.damageMultiplier);
        normalAttackConfig.fixedRange = Mathf.Max(0f, normalAttackConfig.fixedRange);
        normalAttackConfig.rangeMultiplier = Mathf.Max(0f, normalAttackConfig.rangeMultiplier);
        normalMovement.circleSpeedRatio = Mathf.Max(0f, normalMovement.circleSpeedRatio);
        normalMovement.idealRangeRatio = Mathf.Max(0f, normalMovement.idealRangeRatio);
        retreatMovement.safeDistance = Mathf.Max(0f, retreatMovement.safeDistance);
        retreatMovement.retreatStepDistance = Mathf.Max(0f, retreatMovement.retreatStepDistance);
    }
}
