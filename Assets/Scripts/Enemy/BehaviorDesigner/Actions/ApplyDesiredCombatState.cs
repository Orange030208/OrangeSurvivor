using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Enemy/Blackboard")]
[TaskDescription("Applies desired move and attack values from the behavior tree blackboard.")]
public sealed class ApplyDesiredCombatState : EnemyBehaviorDesignerTaskBase
{
    public override TaskStatus OnUpdate()
    {
        if (!HasController())
        {
            return TaskStatus.Failure;
        }

        return behaviorController.ApplyDesiredCombatState() ? TaskStatus.Success : TaskStatus.Failure;
    }
}
