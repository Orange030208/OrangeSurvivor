using System;

public struct PlayerHealthChangedEvent : IGameEvent
{
    public float CurrentHealth;
    public float MaxHealth;

    public PlayerHealthChangedEvent(float currentHealth, float maxHealth)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}

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

public struct RequestPlayerHudSnapshotEvent : IGameEvent
{
}