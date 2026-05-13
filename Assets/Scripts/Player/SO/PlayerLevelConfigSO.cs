using UnityEngine;

[CreateAssetMenu(fileName = "Player Level Config", menuName = ScriptableObjectMenuPaths.PLAYER_LEVEL_CONFIG, order = 0)]
public class PlayerLevelConfigSO : ScriptableObject
{
    private const int MIN_LEVEL = 1;
    private const int MIN_EXPERIENCE = 0;
    private const int MIN_REQUIRED_EXPERIENCE = 1;
    private const int MIN_UPGRADE_POINTS = 1;

    [Header("起始状态")]
    [SerializeField] private int startLevel = MIN_LEVEL;
    [SerializeField] private int startExperience = MIN_EXPERIENCE;

    [Header("成长曲线")]
    [Tooltip("X 为当前等级，Y 为升到下一级所需经验。关键等级之间线性插值，最后一个关键等级之后按最后一段斜率外推。")]
    [SerializeField] private AnimationCurve requiredExperienceByLevel = CreateDefaultRequiredExperienceCurve();
    [SerializeField] private int minimumRequiredExperience = MIN_REQUIRED_EXPERIENCE;
    [SerializeField] private int upgradePointsPerLevel = MIN_UPGRADE_POINTS;

    public int StartLevel => Mathf.Max(MIN_LEVEL, startLevel);
    public int StartExperience => Mathf.Max(MIN_EXPERIENCE, startExperience);
    public AnimationCurve RequiredExperienceByLevel => requiredExperienceByLevel ?? CreateDefaultRequiredExperienceCurve();
    public int MinimumRequiredExperience => Mathf.Max(MIN_REQUIRED_EXPERIENCE, minimumRequiredExperience);
    public int UpgradePointsPerLevel => Mathf.Max(MIN_UPGRADE_POINTS, upgradePointsPerLevel);

    public int GetRequiredExperienceForLevel(int level)
    {
        int normalizedLevel = Mathf.Max(MIN_LEVEL, level);
        AnimationCurve curve = RequiredExperienceByLevel;
        float requiredExperience = EvaluateRequiredExperience(curve, normalizedLevel);
        return Mathf.Max(MinimumRequiredExperience, Mathf.RoundToInt(requiredExperience));
    }

    private void OnValidate()
    {
        startLevel = Mathf.Max(MIN_LEVEL, startLevel);
        startExperience = Mathf.Max(MIN_EXPERIENCE, startExperience);
        requiredExperienceByLevel = NormalizeRequiredExperienceCurve(requiredExperienceByLevel);
        minimumRequiredExperience = Mathf.Max(MIN_REQUIRED_EXPERIENCE, minimumRequiredExperience);
        upgradePointsPerLevel = Mathf.Max(MIN_UPGRADE_POINTS, upgradePointsPerLevel);
    }

    private static float EvaluateRequiredExperience(AnimationCurve curve, int level)
    {
        if (curve == null || curve.length == 0)
        {
            curve = CreateDefaultRequiredExperienceCurve();
        }

        if (level > curve[curve.length - 1].time && curve.length >= 2)
        {
            Keyframe previous = curve[curve.length - 2];
            Keyframe last = curve[curve.length - 1];
            float levelDelta = Mathf.Max(1f, last.time - previous.time);
            float slope = Mathf.Max(0f, (last.value - previous.value) / levelDelta);
            return last.value + slope * (level - last.time);
        }

        return EvaluateLinearBetweenKeys(curve, level);
    }

    private static float EvaluateLinearBetweenKeys(AnimationCurve curve, int level)
    {
        if (level <= curve[0].time)
        {
            return curve[0].value;
        }

        for (int i = 1; i < curve.length; i++)
        {
            Keyframe previous = curve[i - 1];
            Keyframe next = curve[i];
            if (level <= next.time)
            {
                float levelDelta = Mathf.Max(1f, next.time - previous.time);
                float t = Mathf.Clamp01((level - previous.time) / levelDelta);
                return Mathf.Lerp(previous.value, next.value, t);
            }
        }

        return curve[curve.length - 1].value;
    }

    private static AnimationCurve NormalizeRequiredExperienceCurve(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return CreateDefaultRequiredExperienceCurve();
        }

        for (int i = 0; i < curve.length; i++)
        {
            Keyframe key = curve[i];
            key.time = Mathf.Max(MIN_LEVEL, key.time);
            key.value = Mathf.Max(MIN_REQUIRED_EXPERIENCE, key.value);
            curve.MoveKey(i, key);
        }

        return curve;
    }

    private static AnimationCurve CreateDefaultRequiredExperienceCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 8f),
            new Keyframe(2f, 13f),
            new Keyframe(3f, 19f),
            new Keyframe(4f, 26f),
            new Keyframe(5f, 34f),
            new Keyframe(10f, 89f),
            new Keyframe(15f, 169f),
            new Keyframe(20f, 274f));
    }
}
