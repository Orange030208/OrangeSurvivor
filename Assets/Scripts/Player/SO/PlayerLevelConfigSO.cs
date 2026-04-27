using UnityEngine;

[CreateAssetMenu(fileName = "Player Level Config", menuName = ScriptableObjectMenuPaths.PLAYER_LEVEL_CONFIG, order = 0)]
public class PlayerLevelConfigSO : ScriptableObject
{
    private const int MIN_LEVEL = 1;
    private const int MIN_EXPERIENCE = 0;
    private const int MIN_REQUIRED_EXPERIENCE = 1;
    private const int MIN_UPGRADE_POINTS = 1;

    [Header("Start")]
    [SerializeField] private int startLevel = MIN_LEVEL;
    [SerializeField] private int startExperience = MIN_EXPERIENCE;

    [Header("Progression")]
    [SerializeField] private int baseRequiredExperience = 5;
    [SerializeField] private int requiredExperiencePerLevel = 5;
    [SerializeField] private int requiredExperienceGrowthPerLevel = 0;
    [SerializeField] private int minimumRequiredExperience = MIN_REQUIRED_EXPERIENCE;
    [SerializeField] private int upgradePointsPerLevel = MIN_UPGRADE_POINTS;

    public int StartLevel => Mathf.Max(MIN_LEVEL, startLevel);
    public int StartExperience => Mathf.Max(MIN_EXPERIENCE, startExperience);
    public int BaseRequiredExperience => Mathf.Max(MIN_REQUIRED_EXPERIENCE, baseRequiredExperience);
    public int RequiredExperiencePerLevel => Mathf.Max(MIN_EXPERIENCE, requiredExperiencePerLevel);
    public int RequiredExperienceGrowthPerLevel => Mathf.Max(MIN_EXPERIENCE, requiredExperienceGrowthPerLevel);
    public int MinimumRequiredExperience => Mathf.Max(MIN_REQUIRED_EXPERIENCE, minimumRequiredExperience);
    public int UpgradePointsPerLevel => Mathf.Max(MIN_UPGRADE_POINTS, upgradePointsPerLevel);

    private void OnValidate()
    {
        startLevel = Mathf.Max(MIN_LEVEL, startLevel);
        startExperience = Mathf.Max(MIN_EXPERIENCE, startExperience);
        baseRequiredExperience = Mathf.Max(MIN_REQUIRED_EXPERIENCE, baseRequiredExperience);
        requiredExperiencePerLevel = Mathf.Max(MIN_EXPERIENCE, requiredExperiencePerLevel);
        requiredExperienceGrowthPerLevel = Mathf.Max(MIN_EXPERIENCE, requiredExperienceGrowthPerLevel);
        minimumRequiredExperience = Mathf.Max(MIN_REQUIRED_EXPERIENCE, minimumRequiredExperience);
        upgradePointsPerLevel = Mathf.Max(MIN_UPGRADE_POINTS, upgradePointsPerLevel);
    }
}
