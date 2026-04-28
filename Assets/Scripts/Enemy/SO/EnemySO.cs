using UnityEngine;

public abstract class EnemySO : ScriptableObject
{
    [Header("预制体")]
    public Enemy prefab;

    [Header("基础属性")]
    public EnemyRole role;
    [SerializeField] private BasePropGroupSO basePropsAsset;
    [SerializeField] private EntityAnimationConfig animConfig;
    public BasePropGroupSO BasePropsAsset => basePropsAsset;
    public EntityAnimationConfig AnimConfig => animConfig;
}
