using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Attack Config", menuName = "SO/Enemies/Attack/Projectile Attack Config", order = 21)]
public sealed class ProjectileAttackConfigSO : AttackConfigSO
{
    [SerializeField] [Min(0f)] private float damage = 1f;
    [SerializeField] [Min(0.01f)] private float attackFrequency = 1f;
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [SerializeField] private ProjectileFiringMode firingMode = ProjectileFiringMode.Default;
    [SerializeField] private ProjectilePatternConfig patternConfig = default;

    public float Damage => Mathf.Max(0f, damage);
    public float AttackFrequency => Mathf.Max(0.01f, attackFrequency);
    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public ProjectileFiringMode FiringMode => firingMode;
    public ProjectilePatternConfig PatternConfig => patternConfig.Equals(default) ? ProjectilePatternConfig.Default : patternConfig;

    public override Type GetComponentType()
    {
        return typeof(ProjectileAttack);
    }

    public override void ApplyTo(AttackBase attack)
    {
        if (attack is not ProjectileAttack projectileAttack)
        {
            throw new InvalidOperationException($"{nameof(ProjectileAttackConfigSO)} requires {nameof(ProjectileAttack)}.");
        }

        projectileAttack.ApplyConfig(this);
    }
}
