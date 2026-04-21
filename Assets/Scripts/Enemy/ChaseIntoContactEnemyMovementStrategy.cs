using System;

public sealed class ChaseIntoContactEnemyMovementStrategy : IEnemyMovementStrategy
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

        context.Movement.FollowTarget(context.Target, context.DeltaTime, 0f);
    }
}
