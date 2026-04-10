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

public struct MenuStartClickedEvent : IGameEvent
{
}

public struct GameOverRestartClickedEvent : IGameEvent
{
}

public struct GameOverReturnToMenuClickedEvent : IGameEvent
{
}

// 暂停菜单页面内部点击“继续”后的意图事件，交给流程协调器处理。
public struct PauseMenuContinueClickedEvent : IGameEvent
{
}

// 暂停菜单页面内部点击“返回菜单”后的意图事件，交给流程协调器处理。
public struct PauseMenuReturnToMenuClickedEvent : IGameEvent
{
}

// 真正恢复游戏的业务事件，通常在暂停菜单完全关闭后再发出。
public struct ResumeGameRequestedEvent : IGameEvent
{
}

// 真正返回主菜单的业务事件，通常在暂停菜单完全关闭后再发出。
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
