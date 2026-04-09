public struct GameStateChangeRequestEvent : IGameEvent
{
    public GameState TargetState;

    public GameStateChangeRequestEvent(GameState targetState)
    {
        TargetState = targetState;
    }
}

public struct GameStateChangedEvent : IGameEvent
{
    public GameState OldState;
    public GameState NewState;

    public GameStateChangedEvent(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public struct PauseGameRequestedEvent : IGameEvent
{
}

public struct ResumeGameRequestedEvent : IGameEvent
{
}

public struct ReturnToMenuRequestedEvent : IGameEvent
{
}

public struct PauseStateChangedEvent : IGameEvent
{
    public bool IsPaused;

    public PauseStateChangedEvent(bool isPaused)
    {
        IsPaused = isPaused;
    }
}
