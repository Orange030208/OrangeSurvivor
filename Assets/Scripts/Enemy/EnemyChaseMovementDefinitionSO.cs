using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Chase Movement Definition", menuName = "SO/Enemies/Movement/Chase Movement Definition", order = 0)]
public class EnemyChaseMovementDefinitionSO : EnemyMovementDefinitionSO
{
    public override EnemyMovementType MovementType => EnemyMovementType.ChaseIntoContact;

    public override IEnemyMovementStrategy CreateRuntimeStrategy()
    {
        return new ChaseIntoContactEnemyMovementStrategy();
    }
}
