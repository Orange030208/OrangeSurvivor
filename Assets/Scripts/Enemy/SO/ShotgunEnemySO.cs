using UnityEngine;

[CreateAssetMenu(fileName = "ShotgunEnemy", menuName = "Enemy/ShotgunEnemy", order = 4)]
public class ShotgunEnemySO : EnemySO
{
    [Header("Distance")]
    [Min(0f)] public float retreatTriggerDistance = 2.75f;
    [Min(0f)] public float reengageDistance = 4.5f;

    [Header("Strategies")]
    public MovementStrategyBase approachMoveStrategy;
    public MovementStrategyBase attackMoveStrategy;
    public MovementStrategyBase retreatMoveStrategy;
    public AttackStrategyBase attackStrategy;

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        reengageDistance = Mathf.Max(retreatTriggerDistance, reengageDistance);
    }
}
