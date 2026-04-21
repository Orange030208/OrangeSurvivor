using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Stop At Attack Range Movement Definition", menuName = "SO/Enemies/Movement/Stop At Attack Range Movement Definition", order = 1)]
public class EnemyStopAtAttackRangeMovementDefinitionSO : EnemyMovementDefinitionSO
{
    public override EnemyMovementType MovementType => EnemyMovementType.StopAtAttackRange;

    public override IEnemyMovementStrategy CreateRuntimeStrategy()
    {
        return new StopAtAttackRangeEnemyMovementStrategy();
    }
}
