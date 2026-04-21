using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Attack Definition", menuName = "SO/Attack/Projectile Attack Definition", order = 1)]
public class ProjectileAttackDefinitionSO : AttackDefinitionSO
{
    [Header("Projectile")]
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;

    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public Projectile ProjectilePrefab => projectileDefinition != null
        ? ProjectileFactory.ResolveProjectilePrefab(projectileDefinition)
        : throw new MissingReferenceException($"{nameof(ProjectileAttackDefinitionSO)} requires {nameof(projectileDefinition)}.");
    public override AttackType Type => AttackType.Projectile;
}
