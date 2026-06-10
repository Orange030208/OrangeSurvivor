using System;
using System.Collections.Generic;

public readonly struct WaveRoleBudgetSnapshot
{
    public WaveRoleBudgetSnapshot(
        SpawnRole role,
        float targetBudget,
        float spentBudget,
        float deficitBudget,
        float alivePressure,
        int priority)
    {
        Role = role;
        TargetBudget = targetBudget;
        SpentBudget = spentBudget;
        DeficitBudget = deficitBudget;
        AlivePressure = alivePressure;
        Priority = priority;
    }

    public SpawnRole Role { get; }
    public float TargetBudget { get; }
    public float SpentBudget { get; }
    public float DeficitBudget { get; }
    public float AlivePressure { get; }
    public int Priority { get; }
}

public sealed class WaveDirectorSnapshot
{
    public WaveDirectorSnapshot(
        float elapsedTime,
        float expectedSpentBudget,
        float actualSpentBudget,
        float alivePressure,
        IReadOnlyList<WaveRoleBudgetSnapshot> roleSnapshots,
        SpawnReason lastSpawnReason,
        string lastSkipReason)
    {
        ElapsedTime = elapsedTime;
        ExpectedSpentBudget = expectedSpentBudget;
        ActualSpentBudget = actualSpentBudget;
        AlivePressure = alivePressure;
        RoleSnapshots = roleSnapshots ?? Array.Empty<WaveRoleBudgetSnapshot>();
        LastSpawnReason = lastSpawnReason;
        LastSkipReason = lastSkipReason ?? string.Empty;
    }

    public float ElapsedTime { get; }
    public float ExpectedSpentBudget { get; }
    public float ActualSpentBudget { get; }
    public float AlivePressure { get; }
    public IReadOnlyList<WaveRoleBudgetSnapshot> RoleSnapshots { get; }
    public SpawnReason LastSpawnReason { get; }
    public string LastSkipReason { get; }
}
