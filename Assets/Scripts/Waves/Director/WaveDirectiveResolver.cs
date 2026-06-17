using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveDirectiveResolver
{
    public int GetProgressionTotalWaves(StageDirectorProfileSO profile)
    {
        if (profile == null)
        {
            return 1;
        }

        return Mathf.Max(1, profile.FiniteWaveCount);
    }

    public int GetDisplayTotalWaves(StageDirectorProfileSO profile, int currentWaveIndex)
    {
        if (profile == null)
        {
            return 0;
        }

        int finiteCount = Mathf.Max(0, profile.FiniteWaveCount);
        if (profile.Mode == StageDirectorMode.FiniteOnly)
        {
            return finiteCount;
        }

        return Mathf.Max(finiteCount, currentWaveIndex + 1);
    }

    public bool HasNextWave(StageDirectorProfileSO profile, int currentWaveIndex)
    {
        if (profile == null)
        {
            return false;
        }

        if (currentWaveIndex + 1 < profile.FiniteWaveCount)
        {
            return true;
        }

        return profile.SupportsEndless;
    }

    public ResolvedWaveDirective Resolve(StageDirectorProfileSO profile, int waveIndex)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (waveIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(waveIndex));
        }

        if (profile.Mode != StageDirectorMode.EndlessOnly && waveIndex < profile.FiniteWaveCount)
        {
            WaveDirectorDefinitionSO waveDefinition = profile.FiniteWaves[waveIndex];
            if (waveDefinition == null)
            {
                throw new InvalidOperationException($"Missing finite wave definition at index {waveIndex}.");
            }

            return ResolveFiniteWave(profile, waveDefinition, waveIndex);
        }

        if (!profile.SupportsEndless)
        {
            throw new InvalidOperationException($"{nameof(StageDirectorProfileSO)} '{profile.name}' has no endless profile for wave {waveIndex + 1}.");
        }

        int finiteCount = profile.Mode == StageDirectorMode.EndlessOnly ? 0 : profile.FiniteWaveCount;
        int endlessWaveNumber = waveIndex - finiteCount + 1;
        return ResolveEndlessWave(profile, profile.EndlessProfile, waveIndex, endlessWaveNumber);
    }

    private ResolvedWaveDirective ResolveFiniteWave(
        StageDirectorProfileSO profile,
        WaveDirectorDefinitionSO waveDefinition,
        int waveIndex)
    {
        return new ResolvedWaveDirective(
            waveDefinition.WaveId,
            waveDefinition.DisplayName,
            waveIndex + 1,
            false,
            GetProgressionTotalWaves(profile),
            GetDisplayTotalWaves(profile, waveIndex),
            0,
            0,
            waveDefinition.Duration,
            waveDefinition.CompletionMode,
            waveDefinition.TotalBudget,
            waveDefinition.AlivePressureCap,
            waveDefinition.PacingCurve,
            waveDefinition.SpawnRule ?? profile.DefaultSpawnRule,
            ResolveCompositionTargets(waveDefinition.CompositionTargets),
            ResolveRoster(waveDefinition.Roster, waveDefinition.Duration),
            ResolveBeats(waveDefinition.ScriptedBeats, waveDefinition.Duration));
    }

    private ResolvedWaveDirective ResolveEndlessWave(
        StageDirectorProfileSO profile,
        EndlessDirectorProfileSO endlessProfile,
        int globalWaveIndex,
        int endlessWaveNumber)
    {
        EndlessPhaseCardSO[] phaseCards = endlessProfile.PhaseCards;
        if (phaseCards.Length == 0)
        {
            throw new InvalidOperationException($"{nameof(EndlessDirectorProfileSO)} '{endlessProfile.name}' has no phase cards.");
        }

        int phaseIndex = ResolvePhaseIndex(endlessProfile, endlessWaveNumber, phaseCards.Length);
        int endlessLoopIndex = (endlessWaveNumber - 1) / phaseCards.Length;
        EndlessPhaseCardSO phaseCard = phaseCards[phaseIndex];

        float budgetMultiplier = EvaluateGrowth(endlessProfile.BudgetGrowth, endlessLoopIndex);
        float durationMultiplier = EvaluateGrowth(endlessProfile.DurationGrowth, endlessLoopIndex);
        float aliveCapMultiplier = EvaluateGrowth(endlessProfile.AliveCapGrowth, endlessLoopIndex);
        float duration = Mathf.Max(1f, phaseCard.Duration * durationMultiplier);

        List<ResolvedEnemyRosterEntry> roster = ResolveRosterToList(phaseCard.Roster, duration);
        AppendUnlockedRoster(endlessProfile.UnlockRules, endlessWaveNumber, duration, roster);

        List<ResolvedScriptedSpawnBeat> beats = ResolveBeatsToList(phaseCard.ScriptedBeats, duration);
        AppendMilestoneBeats(endlessProfile.MilestoneBeats, endlessWaveNumber, duration, beats);

        return new ResolvedWaveDirective(
            $"{phaseCard.PhaseId}_Endless_{endlessWaveNumber}",
            phaseCard.DisplayName,
            globalWaveIndex + 1,
            true,
            GetProgressionTotalWaves(profile),
            GetDisplayTotalWaves(profile, globalWaveIndex),
            endlessWaveNumber,
            endlessLoopIndex,
            duration,
            phaseCard.CompletionMode,
            phaseCard.TotalBudget * budgetMultiplier,
            phaseCard.AlivePressureCap * aliveCapMultiplier,
            phaseCard.PacingCurve,
            phaseCard.SpawnRule ?? profile.DefaultSpawnRule,
            ResolveCompositionTargets(phaseCard.CompositionTargets),
            roster,
            beats);
    }

    private static int ResolvePhaseIndex(EndlessDirectorProfileSO profile, int endlessWaveNumber, int phaseCount)
    {
        if (profile.PhaseSelectionMode == EndlessPhaseSelectionMode.SeededShuffle)
        {
            System.Random random = new(profile.SelectionSeed + endlessWaveNumber * 17);
            return random.Next(phaseCount);
        }

        return (endlessWaveNumber - 1) % phaseCount;
    }

    private static float EvaluateGrowth(AnimationCurve curve, int endlessLoopIndex)
    {
        if (curve == null)
        {
            return 1f;
        }

        return Mathf.Max(0f, curve.Evaluate(endlessLoopIndex));
    }

    private static IReadOnlyList<ResolvedSpawnRoleTarget> ResolveCompositionTargets(SpawnRoleTarget[] sourceTargets)
    {
        if (sourceTargets == null || sourceTargets.Length == 0)
        {
            return Array.Empty<ResolvedSpawnRoleTarget>();
        }

        float totalShare = 0f;
        for (int i = 0; i < sourceTargets.Length; i++)
        {
            totalShare += Mathf.Max(0f, sourceTargets[i].BudgetShare);
        }

        if (totalShare <= 0f)
        {
            throw new InvalidOperationException("Wave director composition targets must have a positive total budget share.");
        }

        ResolvedSpawnRoleTarget[] resolved = new ResolvedSpawnRoleTarget[sourceTargets.Length];
        for (int i = 0; i < sourceTargets.Length; i++)
        {
            SpawnRoleTarget target = sourceTargets[i];
            float normalizedShare = Mathf.Max(0f, target.BudgetShare) / totalShare;
            bool usesMinBudgetFloor = target.MinBudget > 0f;
            resolved[i] = new ResolvedSpawnRoleTarget(
                target.Role,
                normalizedShare,
                target.MinBudget,
                target.MaxBudget,
                target.Priority,
                usesMinBudgetFloor);
        }

        return resolved;
    }

    private static IReadOnlyList<ResolvedEnemyRosterEntry> ResolveRoster(EnemyRosterEntry[] sourceEntries, float duration)
    {
        return ResolveRosterToList(sourceEntries, duration);
    }

    private static List<ResolvedEnemyRosterEntry> ResolveRosterToList(EnemyRosterEntry[] sourceEntries, float duration)
    {
        List<ResolvedEnemyRosterEntry> resolved = new();
        if (sourceEntries == null)
        {
            return resolved;
        }

        for (int i = 0; i < sourceEntries.Length; i++)
        {
            EnemyRosterEntry entry = sourceEntries[i];
            if (entry == null || !entry.IsValid)
            {
                continue;
            }

            Vector2 activeRange = entry.ActiveTimeRange;
            resolved.Add(new ResolvedEnemyRosterEntry(
                entry.EntryId,
                entry.Enemy,
                entry.Role,
                entry.Tags,
                entry.Cost,
                entry.MinGroupSize,
                entry.MaxGroupSize,
                entry.CooldownSeconds,
                entry.MaxAlive,
                duration * Mathf.Clamp01(activeRange.x / 100f),
                duration * Mathf.Clamp01(activeRange.y / 100f),
                    entry.SpawnRule));
        }

        return resolved;
    }

    private static IReadOnlyList<ResolvedScriptedSpawnBeat> ResolveBeats(ScriptedSpawnBeat[] sourceBeats, float duration)
    {
        return ResolveBeatsToList(sourceBeats, duration);
    }

    private static List<ResolvedScriptedSpawnBeat> ResolveBeatsToList(ScriptedSpawnBeat[] sourceBeats, float duration)
    {
        List<ResolvedScriptedSpawnBeat> resolved = new();
        if (sourceBeats == null)
        {
            return resolved;
        }

        for (int i = 0; i < sourceBeats.Length; i++)
        {
            ScriptedSpawnBeat beat = sourceBeats[i];
            if (beat == null)
            {
                continue;
            }

            List<ResolvedBeatCommand> commands = new();
            EnemySpawnCommandTemplate[] sourceCommands = beat.Commands;
            for (int commandIndex = 0; commandIndex < sourceCommands.Length; commandIndex++)
            {
                EnemySpawnCommandTemplate sourceCommand = sourceCommands[commandIndex];
                if (sourceCommand == null || !sourceCommand.IsValid)
                {
                    continue;
                }

                commands.Add(new ResolvedBeatCommand(
                    sourceCommand.CommandId,
                    sourceCommand.Enemy,
                    sourceCommand.Role,
                    sourceCommand.Tags,
                    sourceCommand.Count,
                    sourceCommand.Cost,
                    sourceCommand.SpawnRule));
            }

            if (commands.Count == 0)
            {
                continue;
            }

            resolved.Add(new ResolvedScriptedSpawnBeat(
                beat.BeatId,
                beat.ResolveTriggerTimeSeconds(duration),
                beat.IgnoreBudget,
                beat.AllowWhenPressureCapped,
                commands));
        }

        resolved.Sort((left, right) => left.TriggerTimeSeconds.CompareTo(right.TriggerTimeSeconds));
        return resolved;
    }

    private static void AppendUnlockedRoster(
        EndlessRosterUnlockRule[] unlockRules,
        int endlessWaveNumber,
        float duration,
        List<ResolvedEnemyRosterEntry> roster)
    {
        if (unlockRules == null)
        {
            return;
        }

        for (int i = 0; i < unlockRules.Length; i++)
        {
            EndlessRosterUnlockRule rule = unlockRules[i];
            if (rule == null || endlessWaveNumber < rule.UnlockEndlessWaveNumber)
            {
                continue;
            }

            List<ResolvedEnemyRosterEntry> resolved = ResolveRosterToList(rule.AdditionalRosterEntries, duration);
            roster.AddRange(resolved);
        }
    }

    private static void AppendMilestoneBeats(
        EndlessMilestoneBeat[] milestoneBeats,
        int endlessWaveNumber,
        float duration,
        List<ResolvedScriptedSpawnBeat> beats)
    {
        if (milestoneBeats == null)
        {
            return;
        }

        for (int i = 0; i < milestoneBeats.Length; i++)
        {
            EndlessMilestoneBeat milestoneBeat = milestoneBeats[i];
            if (milestoneBeat == null || !milestoneBeat.Matches(endlessWaveNumber))
            {
                continue;
            }

            List<ResolvedScriptedSpawnBeat> resolved = ResolveBeatsToList(milestoneBeat.Beats, duration);
            beats.AddRange(resolved);
        }

        beats.Sort((left, right) => left.TriggerTimeSeconds.CompareTo(right.TriggerTimeSeconds));
    }
}
