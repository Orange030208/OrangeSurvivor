using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Run Progression Profile",
    menuName = ScriptableObjectMenuPaths.RUN_PROGRESSION_PROFILE,
    order = 0)]
public sealed class RunProgressionProfileSO : ScriptableObject
{
    private const int MIN_WAVE = 1;
    private const int DEFAULT_AUTHORED_WAVE_COUNT = 20;

    [Header("关卡范围")]
    [SerializeField, Min(MIN_WAVE)] private int authoredWaveCount = DEFAULT_AUTHORED_WAVE_COUNT;

    [Header("难度曲线")]
    [SerializeField] private AnimationCurve difficultyByWave = CreateDifficultyCurve();
    [SerializeField] private List<RunProgressionPropScaleCurve> enemyPropScaleCurves = CreateDefaultEnemyPropScaleCurves();
    [SerializeField, Min(0f)] private float endlessDifficultyMultiplierPerLoop = 1.18f;

    [Header("敌人额外压力")]
    [SerializeField] private List<RunProgressionPropMultiplier> bossPropMultipliers = CreateDefaultBossPropMultipliers();
    [SerializeField] private List<RunProgressionTagPressureRule> tagPressureRules = CreateDefaultTagPressureRules();

    [Header("经济曲线")]
    [SerializeField] private AnimationCurve economyByWave = CreateEconomyCurve();
    [SerializeField] private AnimationCurve shopPriceMultiplierByWave = CreateShopPriceCurve();
    [SerializeField, Min(0f)] private float endlessEconomyMultiplierPerLoop = 1.1f;
    [SerializeField, Min(0f)] private float endlessShopPriceMultiplierPerLoop = 1.16f;

    public int AuthoredWaveCount => Mathf.Max(MIN_WAVE, authoredWaveCount);

    public static RunProgressionProfileSO CreateRuntimeDefault()
    {
        RunProgressionProfileSO profile = ScriptableObject.CreateInstance<RunProgressionProfileSO>();
        profile.name = "Runtime Default Run Progression Profile";
        profile.hideFlags = HideFlags.DontSave;
        return profile;
    }

    public RunProgressionSnapshot Evaluate(int waveNumber, int totalWaves, float runSeconds)
    {
        int safeTotalWaves = Mathf.Max(MIN_WAVE, totalWaves > 0 ? totalWaves : AuthoredWaveCount);
        int safeWave = Mathf.Max(MIN_WAVE, waveNumber);
        int authoredCount = Mathf.Max(MIN_WAVE, Mathf.Min(AuthoredWaveCount, safeTotalWaves));
        float authoredT = authoredCount <= 1 ? 0f : Mathf.Clamp01((safeWave - 1f) / (authoredCount - 1f));
        float endlessProgress = safeWave > authoredCount
            ? (safeWave - authoredCount) / (float)authoredCount
            : 0f;
        int endlessLoop = endlessProgress > 0f ? Mathf.CeilToInt(endlessProgress) : 0;

        float difficulty = Mathf.Max(0f, EvaluateSafe(difficultyByWave, authoredT, 1f));
        float economy = Mathf.Max(0f, EvaluateSafe(economyByWave, authoredT, 1f));
        float shopPrice = Mathf.Max(0f, EvaluateSafe(shopPriceMultiplierByWave, authoredT, 1f));

        if (endlessProgress > 0f)
        {
            difficulty *= Mathf.Pow(Mathf.Max(0f, endlessDifficultyMultiplierPerLoop), endlessProgress);
            economy *= Mathf.Pow(Mathf.Max(0f, endlessEconomyMultiplierPerLoop), endlessProgress);
            shopPrice *= Mathf.Pow(Mathf.Max(0f, endlessShopPriceMultiplierPerLoop), endlessProgress);
        }

        int dangerTier = ResolveDangerTier(safeWave, authoredCount, endlessLoop);
        return new RunProgressionSnapshot(
            safeWave,
            safeTotalWaves,
            Mathf.Max(0f, runSeconds) / 60f,
            endlessLoop,
            difficulty,
            economy,
            shopPrice,
            dangerTier);
    }

    public RunProgressionEnemyScale EvaluateEnemyScale(RunProgressionSnapshot snapshot, EnemySO enemyData)
    {
        float difficulty = Mathf.Max(0f, snapshot.DifficultyCoefficient);
        RunProgressionEnemyScale scale = RunProgressionEnemyScale.Identity;
        IReadOnlyList<RunProgressionPropScaleCurve> propScaleCurves = GetEnemyPropScaleCurves();
        for (int i = 0; i < propScaleCurves.Count; i++)
        {
            RunProgressionPropScaleCurve entry = propScaleCurves[i];
            float multiplier = Mathf.Max(0f, EvaluateSafe(entry.multiplierByDifficulty, difficulty, 1f));
            scale.SetMultiplier(entry.propType, multiplier);
        }

        if (enemyData != null && enemyData.role == EnemyRole.Boss)
        {
            ApplyPropMultipliers(ref scale, GetBossPropMultipliers());
        }

        return scale;
    }

    public RunProgressionEnemyScale EvaluateEnemyScale(
        RunProgressionSnapshot snapshot,
        EnemySO enemyData,
        WaveEnemyTag enemyTags)
    {
        RunProgressionEnemyScale scale = EvaluateEnemyScale(snapshot, enemyData);
        return RunProgressionEnemyScaling.ApplyTagPressure(scale, enemyTags, GetTagPressureRules());
    }

    private void OnValidate()
    {
        authoredWaveCount = Mathf.Max(MIN_WAVE, authoredWaveCount);
        endlessDifficultyMultiplierPerLoop = Mathf.Max(0f, endlessDifficultyMultiplierPerLoop);
        endlessEconomyMultiplierPerLoop = Mathf.Max(0f, endlessEconomyMultiplierPerLoop);
        endlessShopPriceMultiplierPerLoop = Mathf.Max(0f, endlessShopPriceMultiplierPerLoop);
        enemyPropScaleCurves = NormalizePropScaleCurves(enemyPropScaleCurves, CreateDefaultEnemyPropScaleCurves());
        bossPropMultipliers = NormalizePropMultipliers(bossPropMultipliers, CreateDefaultBossPropMultipliers());
        tagPressureRules = NormalizeTagPressureRules(tagPressureRules, CreateDefaultTagPressureRules());
    }

    private static float EvaluateSafe(AnimationCurve curve, float time, float fallback)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
    }

    private static float SanitizeMultiplier(float multiplier, float fallback = 1f)
    {
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
        {
            return Mathf.Max(0f, fallback);
        }

        return Mathf.Max(0f, multiplier);
    }

    private IReadOnlyList<RunProgressionPropScaleCurve> GetEnemyPropScaleCurves()
    {
        return enemyPropScaleCurves != null && enemyPropScaleCurves.Count > 0
            ? enemyPropScaleCurves
            : CreateDefaultEnemyPropScaleCurves();
    }

    private IReadOnlyList<RunProgressionPropMultiplier> GetBossPropMultipliers()
    {
        return bossPropMultipliers != null && bossPropMultipliers.Count > 0
            ? bossPropMultipliers
            : CreateDefaultBossPropMultipliers();
    }

    private IReadOnlyList<RunProgressionTagPressureRule> GetTagPressureRules()
    {
        return tagPressureRules != null && tagPressureRules.Count > 0
            ? tagPressureRules
            : CreateDefaultTagPressureRules();
    }

    private static void ApplyPropMultipliers(
        ref RunProgressionEnemyScale scale,
        IReadOnlyList<RunProgressionPropMultiplier> propMultipliers)
    {
        if (propMultipliers == null)
        {
            return;
        }

        for (int i = 0; i < propMultipliers.Count; i++)
        {
            RunProgressionPropMultiplier entry = propMultipliers[i];
            scale.MultiplyMultiplier(entry.propType, SanitizeMultiplier(entry.multiplier));
        }
    }

    private static List<RunProgressionPropScaleCurve> NormalizePropScaleCurves(
        List<RunProgressionPropScaleCurve> entries,
        List<RunProgressionPropScaleCurve> fallback)
    {
        if (entries == null || entries.Count == 0)
        {
            return fallback;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RunProgressionPropScaleCurve entry = entries[i];
            if (entry.multiplierByDifficulty == null || entry.multiplierByDifficulty.length == 0)
            {
                entry.multiplierByDifficulty = CreateIdentityCurve();
            }
            else
            {
                ClampCurveValuesNonNegative(entry.multiplierByDifficulty);
            }

            entries[i] = entry;
        }

        return entries;
    }

    private static List<RunProgressionPropMultiplier> NormalizePropMultipliers(
        List<RunProgressionPropMultiplier> entries,
        List<RunProgressionPropMultiplier> fallback)
    {
        if (entries == null || entries.Count == 0)
        {
            return fallback;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RunProgressionPropMultiplier entry = entries[i];
            entry.multiplier = SanitizeMultiplier(entry.multiplier);
            entries[i] = entry;
        }

        return entries;
    }

    private static List<RunProgressionTagPressureRule> NormalizeTagPressureRules(
        List<RunProgressionTagPressureRule> entries,
        List<RunProgressionTagPressureRule> fallback)
    {
        if (entries == null || entries.Count == 0)
        {
            return fallback;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RunProgressionTagPressureRule entry = entries[i];
            entry.propMultipliers = NormalizePropMultipliers(entry.propMultipliers, new List<RunProgressionPropMultiplier>());
            entries[i] = entry;
        }

        return entries;
    }

    private static void ClampCurveValuesNonNegative(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            Keyframe key = curve[i];
            float sanitizedValue = SanitizeMultiplier(key.value);
            if (!Mathf.Approximately(key.value, sanitizedValue))
            {
                key.value = sanitizedValue;
                curve.MoveKey(i, key);
            }
        }
    }

    private static int ResolveDangerTier(int waveNumber, int authoredWaveCount, int endlessLoop)
    {
        if (endlessLoop > 0)
        {
            return 4 + endlessLoop;
        }

        float t = authoredWaveCount <= 1 ? 0f : Mathf.Clamp01((waveNumber - 1f) / (authoredWaveCount - 1f));
        if (t >= 0.75f)
        {
            return 3;
        }

        if (t >= 0.5f)
        {
            return 2;
        }

        return t >= 0.25f ? 1 : 0;
    }

    private static AnimationCurve CreateDifficultyCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.21f, 1.25f),
            new Keyframe(0.47f, 1.75f),
            new Keyframe(0.74f, 2.55f),
            new Keyframe(1f, 3.6f));
    }

    private static List<RunProgressionPropScaleCurve> CreateDefaultEnemyPropScaleCurves()
    {
        return new List<RunProgressionPropScaleCurve>
        {
            new(PropType.MaxHealth, CreateHealthCurve()),
            new(PropType.Attack, CreateAttackCurve()),
            new(PropType.MoveSpeed, CreateMoveSpeedCurve()),
            new(PropType.AttackSpeed, CreateAttackSpeedCurve())
        };
    }

    private static List<RunProgressionPropMultiplier> CreateDefaultBossPropMultipliers()
    {
        return new List<RunProgressionPropMultiplier>
        {
            new(PropType.MaxHealth, 1.6f),
            new(PropType.Attack, 1.25f)
        };
    }

    private static List<RunProgressionTagPressureRule> CreateDefaultTagPressureRules()
    {
        return new List<RunProgressionTagPressureRule>
        {
            new(
                WaveEnemyTag.Elite,
                new[]
                {
                    new RunProgressionPropMultiplier(PropType.MaxHealth, 1.3f),
                    new RunProgressionPropMultiplier(PropType.Attack, 1.18f)
                }),
            new(
                WaveEnemyTag.BossLike,
                new[]
                {
                    new RunProgressionPropMultiplier(PropType.MaxHealth, 1.45f),
                    new RunProgressionPropMultiplier(PropType.Attack, 1.18f)
                }),
            new(
                WaveEnemyTag.Ranged,
                new[]
                {
                    new RunProgressionPropMultiplier(PropType.Attack, 1.05f)
                }),
            new(
                WaveEnemyTag.Fast,
                new[]
                {
                    new RunProgressionPropMultiplier(PropType.MoveSpeed, 1.08f)
                })
        };
    }

    private static AnimationCurve CreateIdentityCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 1f));
    }

    private static AnimationCurve CreateHealthCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(1.25f, 1.35f),
            new Keyframe(1.75f, 2.2f),
            new Keyframe(2.55f, 3.8f),
            new Keyframe(3.6f, 6f));
    }

    private static AnimationCurve CreateAttackCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(1.25f, 1.1f),
            new Keyframe(1.75f, 1.32f),
            new Keyframe(2.55f, 1.68f),
            new Keyframe(3.6f, 2.1f));
    }

    private static AnimationCurve CreateMoveSpeedCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(1.75f, 1.04f),
            new Keyframe(2.55f, 1.08f),
            new Keyframe(3.6f, 1.12f));
    }

    private static AnimationCurve CreateAttackSpeedCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(1.75f, 1.08f),
            new Keyframe(2.55f, 1.16f),
            new Keyframe(3.6f, 1.25f));
    }

    private static AnimationCurve CreateEconomyCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.21f, 1.12f),
            new Keyframe(0.47f, 1.35f),
            new Keyframe(0.74f, 1.65f),
            new Keyframe(1f, 2.05f));
    }

    private static AnimationCurve CreateShopPriceCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.21f, 1.15f),
            new Keyframe(0.47f, 1.45f),
            new Keyframe(0.74f, 1.85f),
            new Keyframe(1f, 2.3f));
    }
}
