using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class WaveSpawnDirectorTests
{
    [Test]
    public void PacingCurve_DelaysSpendingUntilCurveAllowsIt()
    {
        WaveSpawnDirector director = new(CreateDirective(
            totalBudget: 10f,
            alivePressureCap: 10f,
            pacingCurve: new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0f),
                new Keyframe(1f, 1f)),
            compositionTargets: new[] { new ResolvedSpawnRoleTarget(SpawnRole.Melee, 1f, 0f, 0f, 0, false) },
            roster: new[] { CreateRosterEntry("melee", SpawnRole.Melee, 1f) }));

        Assert.That(director.TryCreateTickCommand(4f, out EnemySpawnCommand earlyCommand), Is.False);
        Assert.That(earlyCommand, Is.Null);
        Assert.That(director.TryCreateTickCommand(9f, out EnemySpawnCommand lateCommand), Is.True);
        Assert.That(lateCommand.EntryId, Is.EqualTo("melee"));
    }

    [Test]
    public void RoleBudgetDeficit_PrefersRoleWithLargestGap()
    {
        WaveSpawnDirector director = new(CreateDirective(
            totalBudget: 20f,
            alivePressureCap: 20f,
            pacingCurve: AnimationCurve.Linear(0f, 0f, 1f, 1f),
            compositionTargets: new[]
            {
                new ResolvedSpawnRoleTarget(SpawnRole.Melee, 0.5f, 0f, 0f, 0, false),
                new ResolvedSpawnRoleTarget(SpawnRole.Ranged, 0.5f, 0f, 0f, 1, false)
            },
            roster: new[]
            {
                CreateRosterEntry("melee", SpawnRole.Melee, 1f),
                CreateRosterEntry("ranged", SpawnRole.Ranged, 1f)
            }));

        director.CommitSpawn(
            new EnemySpawnCommand("melee", CreateEnemy("melee_enemy"), SpawnRole.Melee, WaveEnemyTag.Normal, 10, 1f, null, SpawnReason.RoleBudgetDeficit, true),
            10f,
            10);

        Assert.That(director.TryCreateTickCommand(10f, out EnemySpawnCommand command), Is.True);
        Assert.That(command.EntryId, Is.EqualTo("ranged"));
    }

    [Test]
    public void AlivePressureCap_BlocksSpawnsUntilPressureDrops()
    {
        WaveSpawnDirector director = new(CreateDirective(
            totalBudget: 10f,
            alivePressureCap: 2f,
            pacingCurve: AnimationCurve.Linear(0f, 0f, 1f, 1f),
            compositionTargets: new[] { new ResolvedSpawnRoleTarget(SpawnRole.Melee, 1f, 0f, 0f, 0, false) },
            roster: new[] { CreateRosterEntry("melee", SpawnRole.Melee, 1f, minGroupSize: 1, maxGroupSize: 2) }));

        Assert.That(director.TryCreateTickCommand(10f, out EnemySpawnCommand firstCommand), Is.True);
        director.CommitSpawn(firstCommand, 10f, 2);

        Assert.That(director.TryCreateTickCommand(10f, out EnemySpawnCommand blockedCommand), Is.False);
        Assert.That(blockedCommand, Is.Null);

        director.NotifyEnemyRemoved("melee", SpawnRole.Melee, 1f);

        Assert.That(director.TryCreateTickCommand(10f, out EnemySpawnCommand recoveredCommand), Is.True);
        Assert.That(recoveredCommand.SpawnCount, Is.EqualTo(1));
    }

    [Test]
    public void ScriptedBeat_TriggersOnceAfterSuccessfulExecution()
    {
        WaveSpawnDirector director = new(CreateDirective(
            totalBudget: 10f,
            alivePressureCap: 10f,
            pacingCurve: AnimationCurve.Linear(0f, 0f, 1f, 1f),
            compositionTargets: new[] { new ResolvedSpawnRoleTarget(SpawnRole.Melee, 1f, 0f, 0f, 0, false) },
            roster: new[] { CreateRosterEntry("melee", SpawnRole.Melee, 1f) },
            beats: new[]
            {
                new ResolvedScriptedSpawnBeat(
                    "beat_0",
                    0f,
                    false,
                    true,
                    new[]
                    {
                        new ResolvedBeatCommand("beat_enemy", CreateEnemy("beat_enemy"), SpawnRole.Elite, WaveEnemyTag.Special, 1, 2f, null)
                    })
            }));

        IReadOnlyList<EnemySpawnCommand> firstCommands = director.CollectReadyBeatCommands(0f, 0.1f);
        Assert.That(firstCommands.Count, Is.EqualTo(1));
        director.CommitSpawn(firstCommands[0], 0.1f, 1);
        director.MarkBeatTriggered("beat_0");

        IReadOnlyList<EnemySpawnCommand> secondCommands = director.CollectReadyBeatCommands(0.1f, 1f);
        Assert.That(secondCommands.Count, Is.EqualTo(0));
    }

    private static ResolvedWaveDirective CreateDirective(
        float totalBudget,
        float alivePressureCap,
        AnimationCurve pacingCurve,
        IReadOnlyList<ResolvedSpawnRoleTarget> compositionTargets,
        IReadOnlyList<ResolvedEnemyRosterEntry> roster,
        IReadOnlyList<ResolvedScriptedSpawnBeat> beats = null)
    {
        return new ResolvedWaveDirective(
            "wave",
            "wave",
            1,
            false,
            1,
            1,
            0,
            0,
            10f,
            WaveCompletionMode.TimerOnly,
            totalBudget,
            alivePressureCap,
            pacingCurve,
            SpawnLocationDefinition.CreateDefault(),
            compositionTargets,
            roster,
            beats ?? System.Array.Empty<ResolvedScriptedSpawnBeat>());
    }

    private static ResolvedEnemyRosterEntry CreateRosterEntry(
        string entryId,
        SpawnRole role,
        float cost,
        int minGroupSize = 1,
        int maxGroupSize = 1)
    {
        return new ResolvedEnemyRosterEntry(
            entryId,
            CreateEnemy(entryId),
            role,
            WaveEnemyTag.Normal,
            cost,
            minGroupSize,
            maxGroupSize,
            0f,
            0,
            0f,
            10f,
            null);
    }

    private static EnemySO CreateEnemy(string name)
    {
        SkeletonEnemySO enemy = ScriptableObject.CreateInstance<SkeletonEnemySO>();
        enemy.name = name;
        return enemy;
    }
}
