using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProjectileFactory
{
    public static Projectile ResolveProjectilePrefab(ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition == null)
        {
            throw new ArgumentNullException(nameof(projectileDefinition), $"{nameof(ProjectileFactory)} requires a non-null {nameof(ProjectileDefinitionSO)}.");
        }

        if (projectileDefinition.ProjectilePrefab == null)
        {
            throw new MissingReferenceException(
                $"{nameof(ProjectileDefinitionSO)} '{projectileDefinition.name}' requires a valid {nameof(Projectile)} prefab reference.");
        }

        return projectileDefinition.ProjectilePrefab;
    }

    public static Projectile CreateProjectile(
        ProjectileDefinitionSO projectileDefinition,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null)
    {
        Projectile projectilePrefab = ResolveProjectilePrefab(projectileDefinition);
        Projectile projectile = Object.Instantiate(projectilePrefab, position, rotation, parent);
        projectile.ApplyProjectileDefinition(projectileDefinition);
        return projectile;
    }
}
