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
    public ProjectileAttackData normalAttackConfig = new()
    {
        timing = new AttackTimingData
        {
            actionId = NORMAL_ATTACK_ACTION_ID,
            attackSfxKey = AudioSfxKey.GunshotLight,
            cooldown = 1f,
            damageMultiplier = 1f,
        },
        detection = new RangeDetectionData
        {
            rangeSource = AttackRangeSource.DetectionRangeProp,
            fixedRange = 7f,
            rangeMultiplier = 1f,
        },
    };

    [Header("Movement")]
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
        normalAttackConfig.timing.actionId = string.IsNullOrWhiteSpace(normalAttackConfig.timing.actionId) ? NORMAL_ATTACK_ACTION_ID : normalAttackConfig.timing.actionId;
        normalAttackConfig.timing.cooldown = Mathf.Max(0f, normalAttackConfig.timing.cooldown);
        normalAttackConfig.timing.damageMultiplier = Mathf.Max(0f, normalAttackConfig.timing.damageMultiplier);
        normalAttackConfig.detection.fixedRange = Mathf.Max(0f, normalAttackConfig.detection.fixedRange);
        normalAttackConfig.detection.rangeMultiplier = Mathf.Max(0f, normalAttackConfig.detection.rangeMultiplier);
        normalMovement.circleSpeedRatio = Mathf.Max(0f, normalMovement.circleSpeedRatio);
        normalMovement.idealRangeRatio = Mathf.Max(0f, normalMovement.idealRangeRatio);
        retreatMovement.safeDistance = Mathf.Max(0f, retreatMovement.safeDistance);
        retreatMovement.retreatStepDistance = Mathf.Max(0f, retreatMovement.retreatStepDistance);
    }
}
