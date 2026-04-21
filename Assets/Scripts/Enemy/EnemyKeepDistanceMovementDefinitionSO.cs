using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Keep Distance Movement Definition", menuName = "SO/Enemies/Movement/Keep Distance Movement Definition", order = 2)]
public class EnemyKeepDistanceMovementDefinitionSO : EnemyMovementDefinitionSO
{
    [Header("Keep Distance")]
    [SerializeField] [Min(0f)] private float desiredDistance = 4f;
    [SerializeField] [Min(0f)] private float tolerance = 0.5f;

    public float DesiredDistance => Mathf.Max(0f, desiredDistance);
    public float Tolerance => Mathf.Max(0f, tolerance);
    public override EnemyMovementType MovementType => EnemyMovementType.StopAtAttackRange;

    public override IEnemyMovementStrategy CreateRuntimeStrategy()
    {
        return new KeepDistanceEnemyMovementStrategy(DesiredDistance, Tolerance);
    }
}
