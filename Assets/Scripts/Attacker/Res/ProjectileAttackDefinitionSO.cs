using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Attack Definition", menuName = "SO/Attack/Projectile Attack Definition", order = 1)]
public class ProjectileAttackDefinitionSO : AttackDefinitionSO
{
    [Header("Projectile")]
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;

    [Header("Pattern")]
    [SerializeField] private ProjectilePatternConfig patternConfig = default;

    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public virtual EnemyProjectileAttackMode AttackMode => EnemyProjectileAttackMode.Single;
    public ProjectilePatternConfig PatternConfig => patternConfig.Equals(default(ProjectilePatternConfig))
        ? ProjectilePatternConfig.Default
        : patternConfig;
    public Projectile ProjectilePrefab => projectileDefinition != null
        ? ProjectileFactory.ResolveProjectilePrefab(projectileDefinition)
        : throw new MissingReferenceException($"{nameof(ProjectileAttackDefinitionSO)} requires {nameof(projectileDefinition)}.");
    public override AttackType Type => AttackType.Projectile;

    public override IEnemyAttackStrategy CreateRuntimeStrategy()
    {
        return new ProjectileEnemyAttackStrategy(this);
    }
}
