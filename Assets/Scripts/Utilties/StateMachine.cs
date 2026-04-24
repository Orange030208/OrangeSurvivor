using System;
using System.Collections.Generic;

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
}

public sealed class StateMachine<TState> where TState : struct, Enum
{
    private readonly Dictionary<TState, StateBase<TState>> states = new();

    public TState CurrentStateKey { get; private set; }
    public StateBase<TState> CurrentState { get; private set; }
    public bool HasState => CurrentState != null;

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
        if (HasState && EqualityComparer<TState>.Default.Equals(CurrentStateKey, newState) && !force)
        {
            return;
        }

        StateBase<TState> nextState = GetState(newState);

        CurrentState?.OnExit();

        CurrentStateKey = newState;
        CurrentState = nextState;
        CurrentState.OnEnter();
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
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
