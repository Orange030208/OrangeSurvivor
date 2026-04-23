using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired move preset to the blackboard.")]
public sealed class SetDesiredMovePreset : EnemyBehaviorDesignerTaskBase
{
    [SerializeField] private MovePresetSO preset;

    public override TaskStatus OnUpdate()
    {
        if (!HasController() || preset == null)
        {
            return TaskStatus.Failure;
        }

        return behaviorController.QueueDesiredMovePreset(preset) ? TaskStatus.Success : TaskStatus.Failure;
    }
}
