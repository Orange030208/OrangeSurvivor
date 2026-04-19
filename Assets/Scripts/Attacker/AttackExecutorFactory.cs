using System;
using UnityEngine;

public static class AttackExecutorFactory
{
    public static IAttackExecutor Create(in AttackExecutorBuildContext context)
    {
        if (context.Owner == null)
        {
            throw new ArgumentNullException(nameof(context), $"{nameof(AttackExecutorFactory)} requires {nameof(AttackExecutorBuildContext.Owner)}.");
        }

        if (context.AttackOrigin == null)
        {
            throw new ArgumentNullException(nameof(context), $"{nameof(AttackExecutorFactory)} requires {nameof(AttackExecutorBuildContext.AttackOrigin)}.");
        }

        if (context.AttackDefinition == null)
        {
            throw new ArgumentNullException(nameof(context), $"{nameof(AttackExecutorFactory)} requires {nameof(AttackExecutorBuildContext.AttackDefinition)}.");
        }

        return context.AttackDefinition.Type switch
        {
            AttackType.Direct => new DirectAttackExecutor(),
            AttackType.Projectile => CreateProjectileExecutor(context.AttackOrigin, context.AttackDefinition as ProjectileAttackDefinitionSO),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.AttackDefinition.Type, "Unsupported attack type.")
        };
    }

    private static IAttackExecutor CreateProjectileExecutor(Transform attackOrigin, ProjectileAttackDefinitionSO attackDefinition)
    {
        if (attackOrigin == null)
        {
            throw new ArgumentNullException(nameof(attackOrigin), $"{nameof(AttackExecutorFactory)} requires {nameof(attackOrigin)}.");
        }

        if (attackDefinition == null)
        {
            throw new ArgumentNullException(nameof(attackDefinition), $"{nameof(AttackExecutorFactory)} requires {nameof(ProjectileAttackDefinitionSO)} for projectile attacks.");
        }

        return new ProjectileAttackExecutor(attackOrigin, attackDefinition.ProjectilePrefab, attackDefinition.ProjectileDefinition);
    }
}
