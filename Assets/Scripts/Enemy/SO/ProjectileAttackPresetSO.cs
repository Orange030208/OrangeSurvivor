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
        return typeof(ProjectileAttack);
    }

    public override void ApplyTo(AttackBase attack, EnemySO enemy)
    {
        if (attack is not ProjectileAttack projectileAttack)
        {
            throw new InvalidOperationException($"{nameof(ProjectileAttackPresetSO)} requires {nameof(ProjectileAttack)}.");
        }

        projectileAttack.SetDamage(damage);
        projectileAttack.SetAttackFrequency(attackFrequency);
        projectileAttack.SetProjectileDefinition(projectileDefinition);
        projectileAttack.SetFiringMode(firingMode);
        projectileAttack.SetPatternConfig(patternConfig);
    }
}
