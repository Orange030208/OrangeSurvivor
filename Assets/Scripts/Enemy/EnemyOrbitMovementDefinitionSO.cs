using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Orbit Movement Definition", menuName = "SO/Enemies/Movement/Orbit Movement Definition", order = 3)]
public class EnemyOrbitMovementDefinitionSO : EnemyMovementDefinitionSO
{
    [Header("Orbit")]
    [SerializeField] [Min(0f)] private float orbitRadius = 3f;
    [SerializeField] [Min(0f)] private float radiusTolerance = 0.35f;
    [SerializeField] private bool clockwise = true;

    public float OrbitRadius => Mathf.Max(0f, orbitRadius);
    public float RadiusTolerance => Mathf.Max(0f, radiusTolerance);
    public bool Clockwise => clockwise;
    public override EnemyMovementType MovementType => EnemyMovementType.StopAtAttackRange;

    public override IEnemyMovementStrategy CreateRuntimeStrategy()
    {
        return new OrbitTargetEnemyMovementStrategy(OrbitRadius, RadiusTolerance, Clockwise);
    }
}
