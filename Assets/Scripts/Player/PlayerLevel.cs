using System;
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [Header("经验")]
    private int requiredXP;
    private int currentXP;
    private int currentLevel = 1;
    private int levelOnWaveStart;
    private int currentUsedLevelUpgradePoints;

    public bool IsLevelUpInCurrentWave => currentLevel > levelOnWaveStart;

    public int LevelUpValue => currentLevel - levelOnWaveStart;
    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int RequiredXP => requiredXP;

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<RequestPlayerHudSnapshotEvent>(PublishSnapshot);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<RequestPlayerHudSnapshotEvent>(PublishSnapshot);
    }

    private void Start()
    {
        RecaclRequiredXP();
        PublishSnapshot();
    }

    private void RecaclRequiredXP()
    {
        requiredXP = currentLevel * 5;
    }

    public void AddXP(int xpToAdd)
    {
        currentXP++;
        GameEventBus.Publish(new PlayerXpChangedEvent(currentXP, requiredXP));
        if (currentXP >= requiredXP)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentUsedLevelUpgradePoints++;

        GameEventBus.Publish(new PlayerLevelChangedEvent(currentLevel));

        currentXP = 0;
        GameEventBus.Publish(new PlayerXpChangedEvent(currentXP, requiredXP));
        RecaclRequiredXP();
    }

    private void OnWaveStarted(WaveStartedEvent wave)
    {
        levelOnWaveStart = currentLevel;
        currentUsedLevelUpgradePoints = 0;
    }

    public int UseUpgradePoints()
    {
        if (currentUsedLevelUpgradePoints > 0)
        {
            currentUsedLevelUpgradePoints--;
        }

        return currentUsedLevelUpgradePoints;
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new PlayerLevelChangedEvent(CurrentLevel));
        GameEventBus.Publish(new PlayerXpChangedEvent(CurrentXP, RequiredXP));
    }
}
