using System;

public struct PlayerLevelChangedEvent : IGameEvent
{
    public int currentLevel;

    public PlayerLevelChangedEvent(int currentLevel)
    {
        this.currentLevel = currentLevel;
    }
}

public struct PlayerXpChangedEvent : IGameEvent
{
    public int currentXP;
    public int requiredXP;

    public PlayerXpChangedEvent(int currentXP, int requiredXP)
    {
        this.currentXP = currentXP;
        this.requiredXP = requiredXP;
    }
}

public struct PlayerMoveInputChangedEvent : IGameEvent
{
    public UnityEngine.Vector2 MoveDirection;

    public PlayerMoveInputChangedEvent(UnityEngine.Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
    }
}
