using System;

public struct PlayerLevelChangedEvent : IGameEvent
{
    public int CurrentLevel;
    public int UnspentUpgradePoints;

    public PlayerLevelChangedEvent(int currentLevel, int unspentUpgradePoints)
    {
        CurrentLevel = currentLevel;
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}

public struct PlayerXpChangedEvent : IGameEvent
{
    public int CurrentXP;
    public int RequiredXP;
    public int UnspentUpgradePoints;

    public PlayerXpChangedEvent(int currentXP, int requiredXP, int unspentUpgradePoints)
    {
        CurrentXP = currentXP;
        RequiredXP = requiredXP;
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}

public struct RequestPlayerLevelSnapshotEvent : IGameEvent
{
}

public struct PlayerMoveInputChangedEvent : IGameEvent
{
    public UnityEngine.Vector2 MoveDirection;

    public PlayerMoveInputChangedEvent(UnityEngine.Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
    }
}
