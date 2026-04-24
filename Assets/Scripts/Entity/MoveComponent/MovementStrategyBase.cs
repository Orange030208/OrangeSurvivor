using UnityEngine;

public abstract class MovementStrategyBase : ScriptableObject
{
    public abstract void ExecuteMove(IMovable movable, Entity self, Entity target, EnemySO enemyData);
}