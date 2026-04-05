public struct RequestWaveHudSnapshotEvent : IGameEvent
{
}

public struct WaveStartedEvent : IGameEvent
{
    public int CurrentWave;
    public int TotalWaves;

    public WaveStartedEvent(int currentWave, int totalWaves)
    {
        CurrentWave = currentWave;
        TotalWaves = totalWaves;
    }
}

public struct WaveCompletedEvent : IGameEvent
{
    public int WaveNumber;

    public WaveCompletedEvent(int waveNumber)
    {
        WaveNumber = waveNumber;
    }
}

public struct AllWavesCompletedEvent : IGameEvent
{
}

public struct WaveProgressEvent : IGameEvent
{
    public float RemainingTime;
    public float TotalTime;

    public WaveProgressEvent(float remainingTime, float totalTime)
    {
        RemainingTime = remainingTime;
        TotalTime = totalTime;
    }
}

public struct RequestUpgradeOptionsSnapshotEvent : IGameEvent
{
}

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public UpgradeProp[] Props;

    public UpgradeOptionsChangedEvent(UpgradeProp[] props)
    {
        Props = props;
    }
}

public struct RequestWeaponSelectionSnapshotEvent : IGameEvent
{
}

public struct WeaponSelectionChangedEvent : IGameEvent
{
    public SelectionWeapon[] SelectionWeapons;

    public WeaponSelectionChangedEvent(SelectionWeapon[] selectionWeapons)
    {
        SelectionWeapons = selectionWeapons;
    }
}

public struct WeaponSelectionOptionClickedEvent : IGameEvent
{
    public int Index;

    public WeaponSelectionOptionClickedEvent(int index)
    {
        Index = index;
    }
}

public struct RequestPlayerHudSnapshotEvent : IGameEvent
{
}

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
    public int CurrentLevel;

    public PlayerLevelChangedEvent(int currentLevel)
    {
        CurrentLevel = currentLevel;
    }
}

public struct PlayerXpChangedEvent : IGameEvent
{
    public int CurrentXP;
    public int RequiredXP;

    public PlayerXpChangedEvent(int currentXP, int requiredXP)
    {
        CurrentXP = currentXP;
        RequiredXP = requiredXP;
    }
}
