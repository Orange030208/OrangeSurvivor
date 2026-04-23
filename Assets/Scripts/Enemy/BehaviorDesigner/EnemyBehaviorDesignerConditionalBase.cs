using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public abstract class EnemyBehaviorDesignerConditionalBase : Conditional
{
    [SerializeField] protected EnemyBehaviorController behaviorController;

    public override void OnAwake()
    {
        if (behaviorController == null)
        {
            behaviorController = GetComponent<EnemyBehaviorController>();
        }
    }

    protected bool HasController()
    {
        return behaviorController != null;
    }
}
