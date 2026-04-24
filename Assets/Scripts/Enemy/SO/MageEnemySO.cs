using UnityEngine;

[CreateAssetMenu(fileName = "MageEnemy", menuName = "Enemy/MageEnemy", order = 0)]
public class MageEnemySO : EnemySO
{
    public float lowHpPercent = 30f; // 低血阈值
    public float idealCircleRange = 6f; // 绕圈理想距离
    public MovementStrategyBase defaultMoveStrategy;
    public AttackStrategyBase defaultAttackStrategy;
    
    public MovementStrategyBase CircleKiteStrategy;
}