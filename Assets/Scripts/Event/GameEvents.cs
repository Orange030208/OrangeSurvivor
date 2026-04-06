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

/// <summary>
/// 由于业务的加载顺序可能快于UI,因此事件可能没有订阅上就触发了,所以重发一份快照帮助ui更新状态
/// </summary>
public struct WaveTransitionSnapshot : IGameEvent
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

public struct AccessorySelectionStartedEvent : IGameEvent
{
    public AccessoryDataSO accessoryData;

    public AccessorySelectionStartedEvent(AccessoryDataSO accessoryData)
    {
        this.accessoryData = accessoryData;
    }
}

public struct AccessoryOperateEvent : IGameEvent
{
    public AccessoryDataSO accessoryData;
    /// <summary>
    /// true为获取,false为回收
    /// </summary>
    public bool selected;

    public AccessoryOperateEvent(AccessoryDataSO accessoryData, bool selected)
    {
        this.accessoryData = accessoryData;
        this.selected = selected;
    }
}

public struct WaveTransitionPhaseChanged : IGameEvent
{
    public TransitionPhase oldPhase;
    public TransitionPhase newPhase;

    public WaveTransitionPhaseChanged(TransitionPhase oldPhase, TransitionPhase newPhase)
    {
        this.oldPhase = oldPhase;
        this.newPhase = newPhase;
    }
}
