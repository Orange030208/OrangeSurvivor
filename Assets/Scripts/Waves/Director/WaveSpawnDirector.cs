using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveSpawnDirector
{
    private readonly ResolvedWaveDirective directive;
    private readonly WaveSpawnDirectorState state = new();

    public WaveSpawnDirector(ResolvedWaveDirective directive)
    {
        this.directive = directive ?? throw new ArgumentNullException(nameof(directive));
    }

    public WaveSpawnDirectorState State => state;

    public IReadOnlyList<EnemySpawnCommand> CollectReadyBeatCommands(float previousTime, float currentTime)
    {
        List<EnemySpawnCommand> commands = new();
        IReadOnlyList<ResolvedScriptedSpawnBeat> beats = directive.ScriptedBeats;
        for (int i = 0; i < beats.Count; i++)
        {
            ResolvedScriptedSpawnBeat beat = beats[i];
            if (state.HasTriggeredBeat(beat.BeatId) || beat.TriggerTimeSeconds > currentTime)
            {
                continue;
            }

            if (!beat.AllowWhenPressureCapped &&
                directive.AlivePressureCap > 0f &&
                state.AlivePressure >= directive.AlivePressureCap)
            {
                state.LastSkipReason = "PressureCapped";
                continue;
            }

            for (int commandIndex = 0; commandIndex < beat.Commands.Count; commandIndex++)
            {
                ResolvedBeatCommand beatCommand = beat.Commands[commandIndex];
                commands.Add(new EnemySpawnCommand(
                    beatCommand.EntryId,
                    beatCommand.Enemy,
                    beatCommand.Role,
                    beatCommand.Tags,
                    beatCommand.Count,
                    beatCommand.UnitCost,
                    beatCommand.SpawnRule,
                    SpawnReason.ScriptedBeat,
                    !beat.IgnoreBudget,
                    beat.BeatId));
            }
        }

        if (commands.Count == 0 && currentTime > previousTime)
        {
            state.LastSkipReason = state.LastSkipReason ?? string.Empty;
        }

        return commands;
    }

    public bool TryCreateTickCommand(float currentTime, out EnemySpawnCommand command)
    {
        command = null;

        float expectedSpentBudget = directive.GetExpectedSpentBudget(currentTime);
        float budgetGap = expectedSpentBudget - state.TotalSpentBudget;
        if (budgetGap <= 0.001f)
        {
            state.LastSkipReason = "BudgetNotReady";
            return false;
        }

        if (directive.AlivePressureCap > 0f && state.AlivePressure >= directive.AlivePressureCap)
        {
            state.LastSkipReason = "PressureCapped";
            return false;
        }

        List<RoleDeficit> deficits = BuildRoleDeficits(currentTime);
        for (int i = 0; i < deficits.Count; i++)
        {
            if (TryCreateCommandForRole(deficits[i], budgetGap, currentTime, out command))
            {
                return true;
            }
        }

        if (TryCreateFallbackCommand(budgetGap, currentTime, out command))
        {
            return true;
        }

        state.LastSkipReason = "NoAvailableEntry";
        return false;
    }

    public void CommitSpawn(EnemySpawnCommand command, float currentTime, int actualSpawnedCount)
    {
        if (command == null || actualSpawnedCount <= 0)
        {
            return;
        }

        float appliedCost = command.UnitCost * actualSpawnedCount;
        if (command.ConsumesBudget)
        {
            state.TotalSpentBudget += appliedCost;
            state.AddSpentBudget(command.Role, appliedCost);
        }

        state.AlivePressure += appliedCost;
        state.AddAlivePressure(command.Role, appliedCost);
        state.AddAliveCount(command.EntryId, actualSpawnedCount);
        state.SetLastSpawnTime(command.EntryId, currentTime);
        state.LastSpawnReason = command.Reason;
        state.LastSkipReason = string.Empty;
    }

    public void MarkBeatTriggered(string beatId)
    {
        state.MarkBeatTriggered(beatId);
    }

    public void NotifyEnemyRemoved(string entryId, SpawnRole role, float unitCost)
    {
        if (string.IsNullOrWhiteSpace(entryId) || unitCost <= 0f)
        {
            return;
        }

        state.AlivePressure = Mathf.Max(0f, state.AlivePressure - unitCost);
        state.AddAlivePressure(role, -unitCost);
        state.AddAliveCount(entryId, -1);
    }

    public WaveDirectorSnapshot CreateSnapshot(float currentTime)
    {
        List<WaveRoleBudgetSnapshot> roles = new();
        List<RoleDeficit> deficits = BuildRoleDeficits(currentTime);
        for (int i = 0; i < deficits.Count; i++)
        {
            RoleDeficit deficit = deficits[i];
            roles.Add(new WaveRoleBudgetSnapshot(
                deficit.Role,
                deficit.TargetBudget,
                state.GetSpentBudget(deficit.Role),
                deficit.DeficitBudget,
                state.GetAlivePressure(deficit.Role),
                deficit.Priority));
        }

        return new WaveDirectorSnapshot(
            currentTime,
            directive.GetExpectedSpentBudget(currentTime),
            state.TotalSpentBudget,
            state.AlivePressure,
            roles,
            state.LastSpawnReason,
            state.LastSkipReason);
    }

    private bool TryCreateCommandForRole(RoleDeficit deficit, float budgetGap, float currentTime, out EnemySpawnCommand command)
    {
        command = null;
        List<ResolvedEnemyRosterEntry> entries = GetAvailableEntriesForRole(deficit.Role, currentTime);
        if (entries.Count == 0)
        {
            return false;
        }

        int cursor = state.GetSelectionCursor(deficit.Role);
        for (int offset = 0; offset < entries.Count; offset++)
        {
            ResolvedEnemyRosterEntry entry = entries[(cursor + offset) % entries.Count];
            if (!TryBuildCommand(entry, budgetGap, currentTime, deficit.Reason, out command))
            {
                continue;
            }

            state.AdvanceSelectionCursor(deficit.Role, entries.Count);
            return true;
        }

        return false;
    }

    private bool TryCreateFallbackCommand(float budgetGap, float currentTime, out EnemySpawnCommand command)
    {
        command = null;
        List<ResolvedEnemyRosterEntry> entries = new();
        for (int i = 0; i < directive.Roster.Count; i++)
        {
            ResolvedEnemyRosterEntry entry = directive.Roster[i];
            if (IsEntryAvailable(entry, currentTime))
            {
                entries.Add(entry);
            }
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.EntryId, right.EntryId));
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnReason reason = string.Equals(state.LastSkipReason, "PressureCapped", StringComparison.Ordinal)
                ? SpawnReason.CatchUpAfterPressureDrop
                : SpawnReason.RoleBudgetDeficit;
            if (TryBuildCommand(entries[i], budgetGap, currentTime, reason, out command))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryBuildCommand(
        ResolvedEnemyRosterEntry entry,
        float budgetGap,
        float currentTime,
        SpawnReason reason,
        out EnemySpawnCommand command)
    {
        command = null;
        if (!IsEntryAvailable(entry, currentTime))
        {
            return false;
        }

        int maxByBudget = Mathf.FloorToInt(Mathf.Max(0f, budgetGap) / entry.UnitCost);
        int maxByAlive = entry.MaxAlive > 0
            ? Mathf.Max(0, entry.MaxAlive - state.GetAliveCount(entry.EntryId))
            : int.MaxValue;
        int maxByPressure = directive.AlivePressureCap > 0f
            ? Mathf.FloorToInt(Mathf.Max(0f, directive.AlivePressureCap - state.AlivePressure) / entry.UnitCost)
            : int.MaxValue;
        int allowed = Mathf.Min(entry.MaxGroupSize, maxByBudget, maxByAlive, maxByPressure);
        if (allowed < entry.MinGroupSize)
        {
            return false;
        }

        command = new EnemySpawnCommand(
            entry.EntryId,
            entry.Enemy,
            entry.Role,
            entry.Tags,
            allowed,
            entry.UnitCost,
            entry.SpawnRule,
            reason,
            true);
        return true;
    }

    private List<RoleDeficit> BuildRoleDeficits(float currentTime)
    {
        List<RoleDeficit> deficits = new();
        float budgetRatio = directive.EvaluateBudgetRatio(currentTime);
        for (int i = 0; i < directive.CompositionTargets.Count; i++)
        {
            ResolvedSpawnRoleTarget target = directive.CompositionTargets[i];
            float totalRoleBudget = directive.TotalBudget * target.NormalizedBudgetShare;
            float minBudget = target.MinBudget;
            bool usesMinBudgetFloor = minBudget > totalRoleBudget;
            totalRoleBudget = Mathf.Max(totalRoleBudget, minBudget);
            if (target.MaxBudget > 0f)
            {
                totalRoleBudget = Mathf.Min(totalRoleBudget, target.MaxBudget);
            }

            float expectedSpent = totalRoleBudget * budgetRatio;
            float deficit = expectedSpent - state.GetSpentBudget(target.Role);
            deficits.Add(new RoleDeficit(
                target.Role,
                deficit,
                totalRoleBudget,
                target.Priority,
                usesMinBudgetFloor ? SpawnReason.MinBudgetRequirement : SpawnReason.RoleBudgetDeficit));
        }

        deficits.Sort(RoleDeficitComparer.Instance);
        return deficits;
    }

    private List<ResolvedEnemyRosterEntry> GetAvailableEntriesForRole(SpawnRole role, float currentTime)
    {
        List<ResolvedEnemyRosterEntry> entries = new();
        for (int i = 0; i < directive.Roster.Count; i++)
        {
            ResolvedEnemyRosterEntry entry = directive.Roster[i];
            if (entry.Role == role && IsEntryAvailable(entry, currentTime))
            {
                entries.Add(entry);
            }
        }

        entries.Sort((left, right) => string.CompareOrdinal(left.EntryId, right.EntryId));
        return entries;
    }

    private bool IsEntryAvailable(ResolvedEnemyRosterEntry entry, float currentTime)
    {
        if (entry == null || entry.Enemy == null || entry.UnitCost <= 0f || !entry.IsActiveAt(currentTime))
        {
            return false;
        }

        if (entry.MaxAlive > 0 && state.GetAliveCount(entry.EntryId) >= entry.MaxAlive)
        {
            return false;
        }

        float lastSpawnTime = state.GetLastSpawnTime(entry.EntryId);
        if (!float.IsNegativeInfinity(lastSpawnTime) &&
            currentTime - lastSpawnTime < entry.CooldownSeconds)
        {
            return false;
        }

        return true;
    }

    private readonly struct RoleDeficit
    {
        public RoleDeficit(
            SpawnRole role,
            float deficitBudget,
            float targetBudget,
            int priority,
            SpawnReason reason)
        {
            Role = role;
            DeficitBudget = deficitBudget;
            TargetBudget = targetBudget;
            Priority = priority;
            Reason = reason;
        }

        public SpawnRole Role { get; }
        public float DeficitBudget { get; }
        public float TargetBudget { get; }
        public int Priority { get; }
        public SpawnReason Reason { get; }
    }

    private sealed class RoleDeficitComparer : IComparer<RoleDeficit>
    {
        public static readonly RoleDeficitComparer Instance = new();

        public int Compare(RoleDeficit left, RoleDeficit right)
        {
            int deficitComparison = right.DeficitBudget.CompareTo(left.DeficitBudget);
            if (deficitComparison != 0)
            {
                return deficitComparison;
            }

            int priorityComparison = right.Priority.CompareTo(left.Priority);
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return left.Role.CompareTo(right.Role);
        }
    }
}
