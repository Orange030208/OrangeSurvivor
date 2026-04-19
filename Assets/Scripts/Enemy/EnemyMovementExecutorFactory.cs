using System;

public static class EnemyMovementExecutorFactory
{
    public static IEnemyMovementExecutor Create(in EnemyMovementExecutorBuildContext context)
    {
        if (context.MovementDefinition == null)
        {
            throw new ArgumentNullException(nameof(context), $"{nameof(EnemyMovementExecutorFactory)} requires {nameof(EnemyMovementExecutorBuildContext.MovementDefinition)}.");
        }

        return context.MovementDefinition.MovementType switch
        {
            EnemyMovementType.ChaseIntoContact => new EnemyChaseMovementExecutor(),
            EnemyMovementType.StopAtAttackRange => new EnemyStopAtAttackRangeMovementExecutor(),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.MovementDefinition.MovementType, "Unsupported enemy movement type.")
        };
    }
}
