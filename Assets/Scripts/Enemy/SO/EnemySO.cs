using UnityEngine;

[System.Serializable]
public struct AttackTimingData
{
    public string actionId;
    public AudioSfxKey attackSfxKey;
    [Min(0f)] public float cooldown;
    [Min(0f)] public float damageMultiplier;
}

[System.Serializable]
public struct RangeDetectionData
{
    public AttackRangeSource rangeSource;
    [Min(0f)] public float fixedRange;
    [Min(0f)] public float rangeMultiplier;
}

[System.Serializable]
public struct ForwardCircleDetectionData
{
    public AttackRangeSource rangeSource;
    [Min(0f)] public float fixedRange;
    [Min(0f)] public float rangeMultiplier;
    [Min(0f)] public float forwardOffset;
}

[System.Serializable]
public struct ProjectileAttackData
{
    public AttackTimingData timing;
    public RangeDetectionData detection;
    public ProjectileDefinitionSO projectileDefinition;
}

[System.Serializable]
public struct DirectDamageAttackData
{
    public AttackTimingData timing;
    public ForwardCircleDetectionData detection;
}

[System.Serializable]
public struct CircleKiteMoveData
{
    [Min(0f)] public float circleSpeedRatio;
    [Min(0f)] public float idealRangeRatio;
}

[System.Serializable]
public struct RetreatMoveData
{
    [Min(0f)] public float safeDistance;
    [Min(0f)] public float retreatStepDistance;
}

public abstract class EnemySO : ScriptableObject
{
    [Header("预制体")]
    public Enemy prefab;

    [Header("基础属性")]
    public EnemyRole role;
    [SerializeField] private BasePropGroupSO basePropsAsset;
    [SerializeField] private EntityAnimationConfig animConfig;
    [SerializeField] private AudioSfxKey damagedSfxKey = AudioSfxKey.None;
    public BasePropGroupSO BasePropsAsset => basePropsAsset;
    public EntityAnimationConfig AnimConfig => animConfig;
    public AudioSfxKey DamagedSfxKey => damagedSfxKey;

}
