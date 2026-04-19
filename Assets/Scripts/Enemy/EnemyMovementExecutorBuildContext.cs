using System;

public readonly struct EnemyMovementExecutorBuildContext
{
    public Enemy Enemy { get; }
    public EnemyMovementDefinitionSO MovementDefinition { get; }

    public EnemyMovementExecutorBuildContext(Enemy enemy, EnemyMovementDefinitionSO movementDefinition)
    {
        Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy), $"{nameof(EnemyMovementExecutorBuildContext)} requires {nameof(enemy)}.");
        MovementDefinition = movementDefinition ?? throw new ArgumentNullException(nameof(movementDefinition), $"{nameof(EnemyMovementExecutorBuildContext)} requires {nameof(movementDefinition)}.");
    }
}
