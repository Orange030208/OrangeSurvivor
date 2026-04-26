using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkirmisherEnemy", menuName = "Enemy/SkirmisherEnemy", order = 3)]
public class SkirmisherEnemySO : EnemySO
{
    [Header("Strategies")]
    public MovementStrategyBase positioningMoveStrategy;
    public AttackStrategyBase attackStrategy;
}
