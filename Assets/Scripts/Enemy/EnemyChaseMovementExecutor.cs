public sealed class EnemyChaseMovementExecutor : IEnemyMovementExecutor
{
    public void Execute(Movement movement, in EnemyMovementContext context)
    {
        movement.FollowPlayer(context.TargetPlayer, context.DeltaTime, 0f);
    }
}
