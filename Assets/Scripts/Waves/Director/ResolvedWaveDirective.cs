using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ResolvedSpawnRoleTarget
{
    public ResolvedSpawnRoleTarget(
        SpawnRole role,
        float normalizedBudgetShare,
        float minBudget,
        float maxBudget,
        int priority,
        bool usesMinBudgetFloor)
    {
        Role = role;
        NormalizedBudgetShare = normalizedBudgetShare;
        MinBudget = minBudget;
        MaxBudget = maxBudget;
        Priority = priority;
        UsesMinBudgetFloor = usesMinBudgetFloor;
    }

    public SpawnRole Role { get; }
    public float NormalizedBudgetShare { get; }
    public float MinBudget { get; }
    public float MaxBudget { get; }
    public int Priority { get; }
    public bool UsesMinBudgetFloor { get; }
}

public sealed class ResolvedEnemyRosterEntry
{
    public ResolvedEnemyRosterEntry(
        string entryId,
        EnemySO enemy,
        SpawnRole role,
        WaveEnemyTag tags,
        float unitCost,
        int minGroupSize,
        int maxGroupSize,
        float cooldownSeconds,
        int maxAlive,
        float activeStartTime,
        float activeEndTime,
        SpawnLocationDefinition spawnRule)
    {
        EntryId = entryId;
        Enemy = enemy;
        Role = role;
        Tags = tags;
        UnitCost = unitCost;
        MinGroupSize = minGroupSize;
        MaxGroupSize = maxGroupSize;
        CooldownSeconds = cooldownSeconds;
        MaxAlive = maxAlive;
        ActiveStartTime = activeStartTime;
        ActiveEndTime = activeEndTime;
        SpawnRule = spawnRule;
    }

    public string EntryId { get; }
    public EnemySO Enemy { get; }
    public SpawnRole Role { get; }
    public WaveEnemyTag Tags { get; }
    public float UnitCost { get; }
    public int MinGroupSize { get; }
    public int MaxGroupSize { get; }
    public float CooldownSeconds { get; }
    public int MaxAlive { get; }
    public float ActiveStartTime { get; }
    public float ActiveEndTime { get; }
    public SpawnLocationDefinition SpawnRule { get; }

    public bool IsActiveAt(float timeSeconds)
    {
        return timeSeconds >= ActiveStartTime && timeSeconds <= ActiveEndTime;
    }
}

public sealed class ResolvedBeatCommand
{
    public ResolvedBeatCommand(
        string entryId,
        EnemySO enemy,
        SpawnRole role,
        WaveEnemyTag tags,
        int count,
        float unitCost,
        SpawnLocationDefinition spawnRule)
    {
        EntryId = entryId;
        Enemy = enemy;
        Role = role;
        Tags = tags;
        Count = count;
        UnitCost = unitCost;
        SpawnRule = spawnRule;
    }

    public string EntryId { get; }
    public EnemySO Enemy { get; }
    public SpawnRole Role { get; }
    public WaveEnemyTag Tags { get; }
    public int Count { get; }
    public float UnitCost { get; }
    public SpawnLocationDefinition SpawnRule { get; }
}

public sealed class ResolvedScriptedSpawnBeat
{
    public ResolvedScriptedSpawnBeat(
        string beatId,
        float triggerTimeSeconds,
        bool ignoreBudget,
        bool allowWhenPressureCapped,
        IReadOnlyList<ResolvedBeatCommand> commands)
    {
        BeatId = beatId;
        TriggerTimeSeconds = triggerTimeSeconds;
        IgnoreBudget = ignoreBudget;
        AllowWhenPressureCapped = allowWhenPressureCapped;
        Commands = commands ?? Array.Empty<ResolvedBeatCommand>();
    }

    public string BeatId { get; }
    public float TriggerTimeSeconds { get; }
    public bool IgnoreBudget { get; }
    public bool AllowWhenPressureCapped { get; }
    public IReadOnlyList<ResolvedBeatCommand> Commands { get; }
}

public sealed class ResolvedWaveDirective
{
    public ResolvedWaveDirective(
        string waveId,
        string displayName,
        int waveNumber,
        bool isEndless,
        int progressionTotalWaves,
        int displayTotalWaves,
        int endlessWaveNumber,
        int endlessLoopIndex,
        float duration,
        WaveCompletionMode completionMode,
        float totalBudget,
        float alivePressureCap,
        AnimationCurve pacingCurve,
        SpawnLocationDefinition defaultSpawnRule,
        IReadOnlyList<ResolvedSpawnRoleTarget> compositionTargets,
        IReadOnlyList<ResolvedEnemyRosterEntry> roster,
        IReadOnlyList<ResolvedScriptedSpawnBeat> scriptedBeats)
    {
        WaveId = waveId;
        DisplayName = displayName;
        WaveNumber = waveNumber;
        IsEndless = isEndless;
        ProgressionTotalWaves = progressionTotalWaves;
        DisplayTotalWaves = displayTotalWaves;
        EndlessWaveNumber = endlessWaveNumber;
        EndlessLoopIndex = endlessLoopIndex;
        Duration = Mathf.Max(1f, duration);
        CompletionMode = completionMode;
        TotalBudget = Mathf.Max(0f, totalBudget);
        AlivePressureCap = Mathf.Max(0f, alivePressureCap);
        PacingCurve = pacingCurve != null ? pacingCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);
        DefaultSpawnRule = defaultSpawnRule ?? SpawnLocationDefinition.CreateDefault();
        CompositionTargets = compositionTargets ?? Array.Empty<ResolvedSpawnRoleTarget>();
        Roster = roster ?? Array.Empty<ResolvedEnemyRosterEntry>();
        ScriptedBeats = scriptedBeats ?? Array.Empty<ResolvedScriptedSpawnBeat>();
    }

    public string WaveId { get; }
    public string DisplayName { get; }
    public int WaveNumber { get; }
    public bool IsEndless { get; }
    public int ProgressionTotalWaves { get; }
    public int DisplayTotalWaves { get; }
    public int EndlessWaveNumber { get; }
    public int EndlessLoopIndex { get; }
    public float Duration { get; }
    public WaveCompletionMode CompletionMode { get; }
    public float TotalBudget { get; }
    public float AlivePressureCap { get; }
    public AnimationCurve PacingCurve { get; }
    public SpawnLocationDefinition DefaultSpawnRule { get; }
    public IReadOnlyList<ResolvedSpawnRoleTarget> CompositionTargets { get; }
    public IReadOnlyList<ResolvedEnemyRosterEntry> Roster { get; }
    public IReadOnlyList<ResolvedScriptedSpawnBeat> ScriptedBeats { get; }

    public float EvaluateBudgetRatio(float elapsedTime)
    {
        float normalizedTime = Duration > 0f ? Mathf.Clamp01(elapsedTime / Duration) : 0f;
        return Mathf.Clamp01(PacingCurve.Evaluate(normalizedTime));
    }

    public float GetExpectedSpentBudget(float elapsedTime)
    {
        return TotalBudget * EvaluateBudgetRatio(elapsedTime);
    }
}
