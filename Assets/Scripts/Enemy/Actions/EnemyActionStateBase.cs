using System;
using UnityEngine;

public abstract class EnemyActionStateBase<TState> : StateBase<TState> where TState : struct, Enum
{
    protected readonly EnemyActionRunner actionRunner = new();
    private bool completionHandled;

    protected EnemyActionStateBase(TState stateKey) : base(stateKey)
    {
    }

    protected bool IsActionComplete => actionRunner.IsComplete;

    public override bool CanExitTo(TState nextState, StateChangeMode mode)
    {
        return mode == StateChangeMode.Force || actionRunner.IsComplete;
    }

    public override void OnExit()
    {
        completionHandled = false;
        actionRunner.Cancel();
    }

    protected void BeginAction(EnemyActionDefinition actionDefinition, IAnimatable animatable)
    {
        completionHandled = false;
        actionRunner.Begin(actionDefinition, animatable);
    }

    protected void TickAction(float deltaTime)
    {
        actionRunner.Tick(deltaTime);
        if (actionRunner.ShouldCommit)
        {
            actionRunner.MarkCommitted();
            OnActionCommit();
        }

        if (actionRunner.IsComplete && !completionHandled)
        {
            completionHandled = true;
            OnActionComplete();
        }
    }

    protected virtual void OnActionCommit()
    {
    }

    protected virtual void OnActionComplete()
    {
    }
}
