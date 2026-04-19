using UnityEngine;

public sealed class DirectAttackExecutor : IAttackExecutor
{
    public void Execute(in AttackContext context)
    {
        if (context.TargetEntity == null)
        {
            return;
        }

        HealthComponent healthComponent = context.TargetEntity.GetComponent<HealthComponent>();
        if (healthComponent == null || healthComponent.OwnerEntity == null)
        {
            return;
        }

        HitService.Apply(new HitRequest(
            context.SourceEntity,
            healthComponent.OwnerEntity,
            context.HitSpec,
            healthComponent.transform.position,
            HitSourceKind.Direct,
            context.SourceEntity != null ? context.SourceEntity.GetType().Name : nameof(DirectAttackExecutor)));
    }
}
