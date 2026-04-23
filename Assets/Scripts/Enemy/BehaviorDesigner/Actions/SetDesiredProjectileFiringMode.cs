using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired projectile firing mode to the blackboard.")]
public sealed class SetDesiredProjectileFiringMode : EnemyBehaviorDesignerTaskBase
{
    [SerializeField] private ProjectileFiringMode firingMode = ProjectileFiringMode.Default;

    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        behaviorController.QueueDesiredProjectileFiringMode(firingMode);
        return TaskStatus.Success;
    }
}
