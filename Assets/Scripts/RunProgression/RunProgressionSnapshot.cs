using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内推进快照。所有数值均是运行时只读结果，具体曲线由 RunProgressionProfileSO 配置。
/// </summary>
public readonly struct RunProgressionSnapshot
{
    public static readonly RunProgressionSnapshot Default = new(
        1,
        1,
        0f,
        0,
        1f,
        1f,
        1f,
        0);

    public int WaveNumber { get; }
    public int TotalWaves { get; }
    public float RunMinutes { get; }
    public int EndlessLoop { get; }
    public float DifficultyCoefficient { get; }
    public float EconomyCoefficient { get; }
    public float ShopPriceMultiplier { get; }
    public int DangerTier { get; }

    public bool IsEndlessWave => TotalWaves > 0 && WaveNumber > TotalWaves;
    public float NormalizedWaveProgress => TotalWaves > 0
        ? Mathf.Clamp01((WaveNumber - 1f) / Mathf.Max(1, TotalWaves - 1f))
        : 0f;

    public RunProgressionSnapshot(
        int waveNumber,
        int totalWaves,
        float runMinutes,
        int endlessLoop,
        float difficultyCoefficient,
        float economyCoefficient,
        float shopPriceMultiplier,
        int dangerTier)
    {
        WaveNumber = Mathf.Max(1, waveNumber);
        TotalWaves = Mathf.Max(0, totalWaves);
        RunMinutes = Mathf.Max(0f, runMinutes);
        EndlessLoop = Mathf.Max(0, endlessLoop);
        DifficultyCoefficient = Mathf.Max(0f, difficultyCoefficient);
        EconomyCoefficient = Mathf.Max(0f, economyCoefficient);
        ShopPriceMultiplier = Mathf.Max(0f, shopPriceMultiplier);
        DangerTier = Mathf.Max(0, dangerTier);
    }
}

public interface IRunProgressionProvider
{
    RunProgressionSnapshot CurrentSnapshot { get; }
}

[Serializable]
public struct RunProgressionPropScaleCurve
{
    public PropType propType;
    public AnimationCurve multiplierByDifficulty;

    public RunProgressionPropScaleCurve(PropType propType, AnimationCurve multiplierByDifficulty)
    {
        this.propType = propType;
        this.multiplierByDifficulty = multiplierByDifficulty;
    }
}

[Serializable]
public struct RunProgressionPropMultiplier
{
    public PropType propType;
    [Min(0f)] public float multiplier;

    public RunProgressionPropMultiplier(PropType propType, float multiplier)
    {
        this.propType = propType;
        this.multiplier = multiplier;
    }
}

[Serializable]
public struct RunProgressionTagPressureRule
{
    public WaveEnemyTag tag;
    public List<RunProgressionPropMultiplier> propMultipliers;

    public RunProgressionTagPressureRule(
        WaveEnemyTag tag,
        IEnumerable<RunProgressionPropMultiplier> propMultipliers)
    {
        this.tag = tag;
        this.propMultipliers = propMultipliers != null
            ? new List<RunProgressionPropMultiplier>(propMultipliers)
            : new List<RunProgressionPropMultiplier>();
    }
}

[Serializable]
public struct RunProgressionEnemyScale
{
    [SerializeField] private List<RunProgressionPropMultiplier> propMultipliers;

    public static RunProgressionEnemyScale Identity => new();

    public readonly IReadOnlyList<RunProgressionPropMultiplier> PropMultipliers =>
        propMultipliers != null
            ? propMultipliers
            : Array.Empty<RunProgressionPropMultiplier>();

    public readonly float GetMultiplier(PropType propType, float fallback = 1f)
    {
        if (propMultipliers == null)
        {
            return Mathf.Max(0f, fallback);
        }

        for (int i = 0; i < propMultipliers.Count; i++)
        {
            RunProgressionPropMultiplier entry = propMultipliers[i];
            if (entry.propType == propType)
            {
                return SanitizeMultiplier(entry.multiplier, fallback);
            }
        }

        return Mathf.Max(0f, fallback);
    }

    public void SetMultiplier(PropType propType, float multiplier)
    {
        EnsureList();
        float safeMultiplier = SanitizeMultiplier(multiplier, 1f);
        for (int i = 0; i < propMultipliers.Count; i++)
        {
            if (propMultipliers[i].propType == propType)
            {
                propMultipliers[i] = new RunProgressionPropMultiplier(propType, safeMultiplier);
                return;
            }
        }

        propMultipliers.Add(new RunProgressionPropMultiplier(propType, safeMultiplier));
    }

    public void MultiplyMultiplier(PropType propType, float multiplier)
    {
        SetMultiplier(propType, GetMultiplier(propType) * SanitizeMultiplier(multiplier, 1f));
    }

    private void EnsureList()
    {
        propMultipliers ??= new List<RunProgressionPropMultiplier>();
    }

    private static float SanitizeMultiplier(float multiplier, float fallback)
    {
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier))
        {
            return Mathf.Max(0f, fallback);
        }

        return Mathf.Max(0f, multiplier);
    }
}
