using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Blackboard")]
[TaskDescription("Refreshes AI facts on the behavior tree blackboard.")]
public sealed class RefreshAiFacts : EnemyBehaviorDesignerTaskBase
{
    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        return behaviorController.RefreshAiFacts(Time.deltaTime) ? TaskStatus.Success : TaskStatus.Failure;
    }
}
