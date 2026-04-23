using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Attack Preset", menuName = "SO/Enemies/Attack/Projectile Attack Preset", order = 21)]
public sealed class ProjectileAttackPresetSO : AttackPresetSO
{
    [Header("Attack")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float attackFrequency = 1f;
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [SerializeField] private ProjectileFiringMode firingMode = ProjectileFiringMode.Default;
    [SerializeField] private ProjectilePatternConfig patternConfig = default;
    public override Type GetComponentType()
    {
        throw new NotImplementedException();
    }

    public override void ApplyTo(AttackBase attack, EnemyDefinitionSO definition)
    {
        throw new NotImplementedException();
    }
}