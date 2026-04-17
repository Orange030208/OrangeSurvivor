using System;
using UnityEngine;

[RequireComponent(typeof(PropertiesManager))]
public class PlayerLevel : MonoBehaviour
{
    private const int MIN_LEVEL = 1;
    private const int MIN_EXPERIENCE = 0;
    private const int DEFAULT_REQUIRED_EXPERIENCE = 1;
    private const float MIN_EXPERIENCE_GAIN_MULTIPLIER = 0f;

    [Header("Config")]
    [SerializeField] private PlayerLevelConfigSO levelConfig;

    private PropertiesManager propertiesManager;
    private int requiredXP;
    private int currentXP;
    private int currentLevel = MIN_LEVEL;
    private int levelOnWaveStart = MIN_LEVEL;
    private int unspentUpgradePoints;

    public bool IsLevelUpInCurrentWave => LevelsGainedInCurrentWave > 0;
    public int LevelUpValue => LevelsGainedInCurrentWave;
    public int LevelsGainedInCurrentWave => Mathf.Max(0, currentLevel - levelOnWaveStart);
    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int RequiredXP => requiredXP;
    public int UnspentUpgradePoints => unspentUpgradePoints;

    private void Awake()
    {
        propertiesManager = GetComponent<PropertiesManager>();
        if (levelConfig == null)
        {
            levelConfig = ResourcesManager.GetPlayerLevelConfig();
        }
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Subscribe<RequestPlayerLevelSnapshotEvent>(PublishSnapshot);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
        GameEventBus.Unsubscribe<RequestPlayerLevelSnapshotEvent>(PublishSnapshot);
    }

    private void Start()
    {
        InitializeProgression();
        PublishSnapshot();
    }

    public void AddXP(int xpToAdd)
    {
        if (xpToAdd <= 0)
        {
            return;
        }

        int appliedExperience = ResolveAppliedExperience(xpToAdd);
        if (appliedExperience <= 0)
        {
            return;
        }

        currentXP += appliedExperience;
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

    private int ResolveAppliedExperience(int rawExperience)
    {
        if (propertiesManager == null)
        {
            return rawExperience;
        }

        float experienceGainMultiplier = Mathf.Max(
            MIN_EXPERIENCE_GAIN_MULTIPLIER,
            propertiesManager.GetPropValue(PropType.ExperienceGain));

        return Mathf.Max(MIN_EXPERIENCE, Mathf.RoundToInt(rawExperience * experienceGainMultiplier));
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
