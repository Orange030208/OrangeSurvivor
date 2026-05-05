using System.Collections.Generic;
using UnityEngine;

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

    public abstract IReadOnlyList<EnemyAttackDefinitionSO> GetAttackDefinitions();

    protected static void AddAttackDefinition(
        List<EnemyAttackDefinitionSO> results,
        EnemyAttackDefinitionSO attackDefinition)
    {
        if (attackDefinition != null && !results.Contains(attackDefinition))
        {
            results.Add(attackDefinition);
        }
    }
}
