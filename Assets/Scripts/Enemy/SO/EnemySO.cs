using UnityEngine;

[System.Serializable]
public enum EnemyMovementPattern
{
    None = 0,
    DirectChase = 1,
    CircleKite = 2,
    Retreat = 3
}

[System.Serializable]
public struct EnemyMovementConfig
{
    public EnemyMovementPattern pattern;
    [Min(0f)] public float circleSpeedRatio;
    [Min(0f)] public float idealRangeRatio;
    [Min(0f)] public float safeDistance;
    [Min(0f)] public float retreatStepDistance;
}

[System.Serializable]
public struct EnemyAttackConfig
{
    public string actionId;
    public AudioSfxKey attackSfxKey;
    [Min(0f)] public float cooldown;
    [Min(0f)] public float damageMultiplier;
    public AttackRangeSource rangeSource;
    [Min(0f)] public float fixedRange;
    [Min(0f)] public float rangeMultiplier;
    [Min(0f)] public float forwardOffset;
    public ProjectileDefinitionSO projectileDefinition;
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
