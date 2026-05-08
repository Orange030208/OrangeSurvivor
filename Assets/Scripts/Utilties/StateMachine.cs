using System;
using System.Collections.Generic;

public enum StateChangeMode
{
    Normal,
    Force
}

public enum StateChangeResult
{
    Changed,
    Deferred,
    Ignored
}

public abstract class StateBase<TState> where TState : struct, Enum
{
    protected StateBase(TState stateKey)
    {
        StateKey = stateKey;
    }

    public TState StateKey { get; }

    public virtual void OnEnter()
    {
    }

    public virtual void OnExit()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnFixedUpdate()
    {
    }

    public virtual bool CanExitTo(TState nextState, StateChangeMode mode)
    {
        return true;
    }
}

public sealed class StateMachine<TState> where TState : struct, Enum
{
    private readonly Dictionary<TState, StateBase<TState>> states = new();

    private bool hasPendingState;
    private TState pendingStateKey;

    public TState CurrentStateKey { get; private set; }
    public StateBase<TState> CurrentState { get; private set; }
    public bool HasState => CurrentState != null;
    public bool HasPendingState => hasPendingState;

    public void RegisterState(StateBase<TState> state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        states[state.StateKey] = state;
    }

    public void ChangeState(TState newState, bool force = false)
    {
        RequestState(newState, force ? StateChangeMode.Force : StateChangeMode.Normal);
    }

    public StateChangeResult RequestState(TState newState, StateChangeMode mode = StateChangeMode.Normal)
    {
        bool force = mode == StateChangeMode.Force;
        if (HasState && EqualityComparer<TState>.Default.Equals(CurrentStateKey, newState) && !force)
        {
            return StateChangeResult.Ignored;
        }

        StateBase<TState> nextState = GetState(newState);
        if (HasState && !force && !CurrentState.CanExitTo(newState, mode))
        {
            pendingStateKey = newState;
            hasPendingState = true;
            return StateChangeResult.Deferred;
        }

        ChangeStateInternal(newState, nextState);
        return StateChangeResult.Changed;
    }

    public bool TryApplyPendingState()
    {
        if (!hasPendingState || CurrentState == null)
        {
            return false;
        }

        TState nextState = pendingStateKey;
        if (!CurrentState.CanExitTo(nextState, StateChangeMode.Normal))
        {
            return false;
        }

        ChangeStateInternal(nextState, GetState(nextState));
        return true;
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
        TryApplyPendingState();
    }

    public void FixedUpdate()
    {
        CurrentState?.OnFixedUpdate();
    }

    public bool IsCurrentState(TState state)
    {
        return HasState && EqualityComparer<TState>.Default.Equals(CurrentStateKey, state);
    }

    public void ClearState(bool runExit = false)
    {
        if (CurrentState == null)
        {
            return;
        }

        if (runExit)
        {
            CurrentState.OnExit();
        }

        CurrentState = null;
        hasPendingState = false;
    }

    private void ChangeStateInternal(TState newState, StateBase<TState> nextState)
    {
        hasPendingState = false;
        CurrentState?.OnExit();

        CurrentStateKey = newState;
        CurrentState = nextState;
        CurrentState.OnEnter();
    }

    private StateBase<TState> GetState(TState state)
    {
        if (!states.TryGetValue(state, out StateBase<TState> stateInstance))
        {
            throw new InvalidOperationException($"State '{state}' has not been registered in {GetType().Name}.");
        }

        return stateInstance;
    }
}
