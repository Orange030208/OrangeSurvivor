using UnityEngine;

[CreateAssetMenu(fileName = "MeleeEnemy", menuName = "Enemy/MeleeEnemy", order = 2)]
public class MeleeEnemySO : EnemySO
{
    [Header("Strategies")]
    public MovementStrategyBase chaseMoveStrategy;
    public AttackStrategyBase attackStrategy;
}
