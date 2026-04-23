using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Conditionals")]
[TaskDescription("Returns success if the enemy has a valid target.")]
public sealed class HasValidTarget : EnemyBehaviorDesignerConditionalBase
{
    public override TaskStatus OnUpdate()
    {
        Debug.Log($"{Owner.name}");
        return HasController() && behaviorController.HasValidTarget() ? TaskStatus.Success : TaskStatus.Failure;
    }
}
