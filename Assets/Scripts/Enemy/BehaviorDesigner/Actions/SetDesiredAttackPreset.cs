using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Enemy/Desired")]
[TaskDescription("Writes the desired attack preset to the blackboard.")]
public sealed class SetDesiredAttackPreset : EnemyBehaviorDesignerTaskBase
{
    [SerializeField] private AttackPresetSO preset;

    public override TaskStatus OnUpdate()
    {
        if (!HasController() || preset == null)
        {
            return TaskStatus.Failure;
        }

        return behaviorController.QueueDesiredAttackPreset(preset) ? TaskStatus.Success : TaskStatus.Failure;
    }
}
