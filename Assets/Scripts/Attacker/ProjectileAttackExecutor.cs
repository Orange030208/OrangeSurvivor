using System;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ProjectileAttackExecutor : IAttackExecutor, IProjectileLauncher
{
    private readonly Transform shootingPoint;
    private readonly Projectile projectilePrefab;
    private readonly ProjectileDefinitionSO projectileDefinition;

    public ProjectileAttackExecutor(Transform shootingPoint, Projectile projectilePrefab, ProjectileDefinitionSO projectileDefinition)
    {
        this.shootingPoint = shootingPoint != null
            ? shootingPoint
            : throw new ArgumentNullException(nameof(shootingPoint), $"{nameof(ProjectileAttackExecutor)} requires {nameof(shootingPoint)}.");
        this.projectilePrefab = projectilePrefab ?? throw new ArgumentNullException(nameof(projectilePrefab), $"{nameof(ProjectileAttackExecutor)} requires {nameof(projectilePrefab)}.");
        this.projectileDefinition = projectileDefinition;
    }

    public void Execute(in AttackContext context)
    {
        if (context.TargetEntity == null)
        {
            return;
        }

        Projectile projectile = Object.Instantiate(projectilePrefab, shootingPoint.position, Quaternion.identity);
        LayerMask targetLayerMask = BuildTargetLayerMask(context.TargetEntity);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            null,
            context.SourceEntity,
            shootingPoint.position,
            context.AimDirection,
            context.HitSpec,
            targetLayerMask,
            0,
            projectileDefinition,
            0,
            ProjectileFiringMode.Default));
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile), $"{nameof(ProjectileAttackExecutor)} requires a valid {nameof(IProjectile)} instance.");
        }

        if (context.ProjectileDefinition != null)
        {
            AudioSfxBridge.RequestPlay(context.ProjectileDefinition.LaunchSfxKey);
            ApplyProjectilePresentation(projectile, context.ProjectileDefinition);
        }

        projectile.Launch(context);
    }

    public void OnProjectileHit(HitRequest hitRequest, IProjectile projectile)
    {
        
    }

    private static void ApplyProjectilePresentation(IProjectile projectile, ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition.TemplateKind != ProjectileTemplateKind.Common)
        {
            return;
        }

        EntityRenderer entityRenderer = projectile.EntityRenderer;
        if (entityRenderer == null)
        {
            return;
        }

        entityRenderer.SetSprite(projectileDefinition.Icon);
    }

    private static LayerMask BuildTargetLayerMask(Entity targetEntity)
    {
        if (targetEntity == null)
        {
            throw new ArgumentNullException(nameof(targetEntity), $"{nameof(ProjectileAttackExecutor)} requires {nameof(Entity)} target to build target layer mask.");
        }

        return 1 << targetEntity.gameObject.layer;
    }
}
