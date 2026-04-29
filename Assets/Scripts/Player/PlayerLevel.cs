using System;
using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
public class PlayerLevel : EntityComponentBase
{
    private const int MIN_LEVEL = 1;
    private const int MIN_EXPERIENCE = 0;
    private const int DEFAULT_REQUIRED_EXPERIENCE = 1;

    [Header("Config")]
    [SerializeField] private PlayerLevelConfigSO levelConfig;

    private Entity owner;
    private PropertiesManager propertiesManager;
    private int requiredXP;
    private int currentXP;
    private int currentLevel = MIN_LEVEL;
    private int levelOnWaveStart = MIN_LEVEL;
    private int unspentUpgradePoints;
    private float pendingExperienceGain;

    public override Entity Owner => owner;

    public bool IsLevelUpInCurrentWave => LevelsGainedInCurrentWave > 0;
    public int LevelUpValue => LevelsGainedInCurrentWave;
    public int LevelsGainedInCurrentWave => Mathf.Max(0, currentLevel - levelOnWaveStart);
    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int RequiredXP => requiredXP;
    public int UnspentUpgradePoints => unspentUpgradePoints;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        propertiesManager = GetComponent<PropertiesManager>();
        if (levelConfig == null)
        {
            levelConfig = ResourcesManager.GetPlayerLevelConfig();
        }

        InitializeProgression();
        PublishSnapshot();
    }

    public override void OnEnableComponent()
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<RequestPlayerLevelSnapshotEvent>(PublishSnapshot);
    }

    public override void OnDisableComponent()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<RequestPlayerLevelSnapshotEvent>(PublishSnapshot);
    }

    public void AddXP(int xpToAdd)
    {
        int resolvedXp = ResolveExperienceGain(xpToAdd);
        if (resolvedXp <= 0)
        {
            return;
        }

        currentXP += resolvedXp;
        ResolvePendingLevelUps();
        PublishSnapshot();
    }

    public int ConsumeUpgradePoint()
    {
        if (unspentUpgradePoints <= 0)
        {
            return 0;
        }

        unspentUpgradePoints--;
        PublishSnapshot();
        return unspentUpgradePoints;
    }

    private void InitializeProgression()
    {
        currentLevel = GetConfiguredStartLevel();
        currentXP = Mathf.Max(MIN_EXPERIENCE, GetConfiguredStartExperience());
        levelOnWaveStart = currentLevel;
        unspentUpgradePoints = MIN_EXPERIENCE;
        pendingExperienceGain = 0f;
        requiredXP = CalculateRequiredXP(currentLevel);
        ResolvePendingLevelUps();
    }

    private void ResolvePendingLevelUps()
    {
        int safetyCounter = MIN_EXPERIENCE;
        while (currentXP >= requiredXP && requiredXP > MIN_EXPERIENCE)
        {
            currentXP -= requiredXP;
            LevelUp();
            safetyCounter++;
            if (safetyCounter > 1000)
            {
                Debug.LogError("[PlayerLevel] ResolvePendingLevelUps exceeded safety limit.");
                break;
            }
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        unspentUpgradePoints += GetUpgradePointsPerLevel();
        requiredXP = CalculateRequiredXP(currentLevel);
        GameEventBus.Publish(new PlayerLevelChangedEvent(currentLevel, unspentUpgradePoints));
    }

    private int CalculateRequiredXP(int level)
    {
        PlayerLevelConfigSO config = levelConfig;
        if (config == null)
        {
            return DEFAULT_REQUIRED_EXPERIENCE;
        }

        int normalizedLevel = Mathf.Max(MIN_LEVEL, level);
        int levelOffset = normalizedLevel - MIN_LEVEL;
        int incrementalRequirement = config.RequiredExperiencePerLevel * levelOffset;
        int growthRequirement = config.RequiredExperienceGrowthPerLevel * levelOffset * Mathf.Max(0, levelOffset - 1) / 2;
        int totalRequirement = config.BaseRequiredExperience + incrementalRequirement + growthRequirement;
        return Mathf.Max(config.MinimumRequiredExperience, totalRequirement);
    }

    private int ResolveExperienceGain(int baseXp)
    {
        if (baseXp <= 0)
        {
            return 0;
        }

        float bonus = propertiesManager != null
            ? Mathf.Max(0f, propertiesManager.GetPropValue(PropType.ExperienceGain))
            : 0f;
        pendingExperienceGain += baseXp * (1f + bonus);
        int resolvedXp = Mathf.FloorToInt(pendingExperienceGain);
        pendingExperienceGain -= resolvedXp;
        return Mathf.Max(0, resolvedXp);
    }

    private int GetConfiguredStartLevel()
    {
        return levelConfig != null ? levelConfig.StartLevel : MIN_LEVEL;
    }

    private int GetConfiguredStartExperience()
    {
        return levelConfig != null ? levelConfig.StartExperience : MIN_EXPERIENCE;
    }

    private int GetUpgradePointsPerLevel()
    {
        return levelConfig != null ? levelConfig.UpgradePointsPerLevel : 1;
    }

    private void OnWaveStarted(WaveStartedEvent _)
    {
        levelOnWaveStart = currentLevel;
    }

    private void PublishSnapshot()
    {
        GameEventBus.Publish(new PlayerLevelChangedEvent(CurrentLevel, UnspentUpgradePoints));
        GameEventBus.Publish(new PlayerXpChangedEvent(CurrentXP, RequiredXP, UnspentUpgradePoints));
    }
}
