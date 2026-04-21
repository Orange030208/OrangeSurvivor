using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Dash Approach Movement Definition", menuName = "SO/Enemies/Movement/Dash Approach Movement Definition", order = 4)]
public class EnemyDashApproachMovementDefinitionSO : EnemyMovementDefinitionSO
{
    [Header("Dash")]
    [SerializeField] [Min(0f)] private float dashSpeedMultiplier = 2.5f;
    [SerializeField] [Min(0.01f)] private float dashDuration = 0.3f;
    [SerializeField] [Min(0.01f)] private float dashCooldown = 1.5f;
    [SerializeField] [Min(0f)] private float dashTriggerDistance = 4f;
    [SerializeField] [Min(0f)] private float stopDistance = 0.5f;

    public float DashSpeedMultiplier => Mathf.Max(0f, dashSpeedMultiplier);
    public float DashDuration => Mathf.Max(0.01f, dashDuration);
    public float DashCooldown => Mathf.Max(0.01f, dashCooldown);
    public float DashTriggerDistance => Mathf.Max(0f, dashTriggerDistance);
    public float StopDistance => Mathf.Max(0f, stopDistance);
    public override EnemyMovementType MovementType => EnemyMovementType.ChaseIntoContact;

    public override IEnemyMovementStrategy CreateRuntimeStrategy()
    {
        return new DashApproachEnemyMovementStrategy(DashSpeedMultiplier, DashDuration, DashCooldown, DashTriggerDistance, StopDistance);
    }
}
