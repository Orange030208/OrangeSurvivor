using UnityEngine;

public abstract class EnemySO : ScriptableObject
{
    [Header("预制体")]
    public Enemy prefab;

    [Header("基础属性")]
    public EnemyRole role;
    [SerializeField] private BasePropGroupSO basePropsAsset;
    [SerializeField] private EnemyAnimationConfig animConfig;
    public BasePropGroupSO BasePropsAsset => basePropsAsset;
    public EnemyAnimationConfig AnimConfig => animConfig;
}
