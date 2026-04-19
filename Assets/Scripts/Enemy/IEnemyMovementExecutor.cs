public interface IEnemyMovementExecutor
{
    void Execute(Movement movement, in EnemyMovementContext context);
}
