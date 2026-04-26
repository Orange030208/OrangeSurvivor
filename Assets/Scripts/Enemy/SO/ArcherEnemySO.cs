using UnityEngine;

[CreateAssetMenu(fileName = "ArcherEnemy", menuName = "Enemy/ArcherEnemy", order = 1)]
public class ArcherEnemySO : EnemySO
{
    [Header("Distance")]
    [Min(0f)] public float retreatTriggerDistance = 4f;
    [Min(0f)] public float retreatCompleteDistance = 7f;

    [Header("Strategies")]
    public MovementStrategyBase approachMoveStrategy;
    public MovementStrategyBase retreatMoveStrategy;
    public AttackStrategyBase attackStrategy;
    public AttackStrategyBase retreatAttackStrategy;

    private void OnValidate()
    {
        retreatTriggerDistance = Mathf.Max(0f, retreatTriggerDistance);
        retreatCompleteDistance = Mathf.Max(retreatTriggerDistance, retreatCompleteDistance);
    }
}
