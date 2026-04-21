using System;

public sealed class StopAtAttackRangeEnemyMovementStrategy : IEnemyMovementStrategy
{
    public void Tick(EnemyMovementContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Movement == null || context.Target == null)
        {
            return;
        }

        context.Movement.FollowTarget(context.Target, context.DeltaTime, context.AttackDetectionRadius);
    }
}
