public sealed class EnemyStopAtAttackRangeMovementExecutor : IEnemyMovementExecutor
{
    public void Execute(Movement movement, in EnemyMovementContext context)
    {
        movement.FollowPlayer(context.TargetPlayer, context.DeltaTime, context.AttackDetectionRadius);
    }
}
