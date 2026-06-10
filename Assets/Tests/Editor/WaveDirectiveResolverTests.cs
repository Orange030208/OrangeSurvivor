using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class WaveDirectiveResolverTests
{
    [Test]
    public void FiniteOnly_LastWaveHasNoNextWave()
    {
        StageDirectorProfileSO profile = ScriptableObject.CreateInstance<StageDirectorProfileSO>();
        SetField(profile, "mode", StageDirectorMode.FiniteOnly);
        SetField(profile, "finiteWaves", new[] { CreateFiniteWave("finite_wave") });

        WaveDirectiveResolver resolver = new();

        Assert.That(resolver.HasNextWave(profile, 0), Is.False);
        ResolvedWaveDirective directive = resolver.Resolve(profile, 0);
        Assert.That(directive.IsEndless, Is.False);
        Assert.That(directive.WaveId, Is.EqualTo("finite_wave"));
    }

    [Test]
    public void FiniteThenEndless_ResolvesEndlessWaveAfterFiniteList()
    {
        StageDirectorProfileSO profile = ScriptableObject.CreateInstance<StageDirectorProfileSO>();
        SetField(profile, "mode", StageDirectorMode.FiniteThenEndless);
        SetField(profile, "finiteWaves", new[] { CreateFiniteWave("finite_wave") });
        SetField(profile, "endlessProfile", CreateEndlessProfile(
            CreatePhaseCard("phase_a", 30f),
            CreatePhaseCard("phase_b", 40f)));

        WaveDirectiveResolver resolver = new();

        ResolvedWaveDirective finiteDirective = resolver.Resolve(profile, 0);
        ResolvedWaveDirective endlessDirective = resolver.Resolve(profile, 1);

        Assert.That(finiteDirective.IsEndless, Is.False);
        Assert.That(endlessDirective.IsEndless, Is.True);
        Assert.That(endlessDirective.EndlessWaveNumber, Is.EqualTo(1));
        Assert.That(endlessDirective.WaveId, Does.StartWith("phase_a_Endless_1"));
        Assert.That(resolver.HasNextWave(profile, 0), Is.True);
        Assert.That(resolver.HasNextWave(profile, 1), Is.True);
    }

    [Test]
    public void EndlessGrowth_AppliesLoopMultipliersToResolvedWave()
    {
        EndlessDirectorProfileSO endlessProfile = CreateEndlessProfile(CreatePhaseCard("phase_a", 30f));
        SetField(endlessProfile, "budgetGrowth", new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(2f, 3f)));
        SetField(endlessProfile, "durationGrowth", new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(2f, 2f)));
        SetField(endlessProfile, "aliveCapGrowth", new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(2f, 4f)));

        StageDirectorProfileSO profile = ScriptableObject.CreateInstance<StageDirectorProfileSO>();
        SetField(profile, "mode", StageDirectorMode.EndlessOnly);
        SetField(profile, "finiteWaves", System.Array.Empty<WaveDirectorDefinitionSO>());
        SetField(profile, "endlessProfile", endlessProfile);

        WaveDirectiveResolver resolver = new();
        ResolvedWaveDirective directive = resolver.Resolve(profile, 2);

        Assert.That(directive.IsEndless, Is.True);
        Assert.That(directive.EndlessWaveNumber, Is.EqualTo(3));
        Assert.That(directive.EndlessLoopIndex, Is.EqualTo(2));
        Assert.That(directive.TotalBudget, Is.EqualTo(30f));
        Assert.That(directive.Duration, Is.EqualTo(60f));
        Assert.That(directive.AlivePressureCap, Is.EqualTo(40f));
    }

    private static WaveDirectorDefinitionSO CreateFiniteWave(string waveId)
    {
        WaveDirectorDefinitionSO wave = ScriptableObject.CreateInstance<WaveDirectorDefinitionSO>();
        SetField(wave, "waveId", waveId);
        SetField(wave, "displayName", waveId);
        SetField(wave, "duration", 30f);
        SetField(wave, "completionMode", WaveCompletionMode.TimerOnly);
        SetField(wave, "totalBudget", 10f);
        SetField(wave, "alivePressureCap", 5f);
        SetField(wave, "pacingCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
        SetField(wave, "compositionTargets", new[] { CreateRoleTarget(SpawnRole.Melee, 1f) });
        SetField(wave, "roster", new[] { CreateRosterEntry("finite_enemy", SpawnRole.Melee) });
        SetField(wave, "scriptedBeats", System.Array.Empty<ScriptedSpawnBeat>());
        return wave;
    }

    private static EndlessDirectorProfileSO CreateEndlessProfile(params EndlessPhaseCardSO[] cards)
    {
        EndlessDirectorProfileSO profile = ScriptableObject.CreateInstance<EndlessDirectorProfileSO>();
        SetField(profile, "phaseCards", cards);
        SetField(profile, "phaseSelectionMode", EndlessPhaseSelectionMode.Sequence);
        SetField(profile, "selectionSeed", 1);
        SetField(profile, "budgetGrowth", EndlessDirectorProfileSO.CreateFlatGrowthCurve());
        SetField(profile, "durationGrowth", EndlessDirectorProfileSO.CreateFlatGrowthCurve());
        SetField(profile, "aliveCapGrowth", EndlessDirectorProfileSO.CreateFlatGrowthCurve());
        SetField(profile, "unlockRules", System.Array.Empty<EndlessRosterUnlockRule>());
        SetField(profile, "milestoneBeats", System.Array.Empty<EndlessMilestoneBeat>());
        return profile;
    }

    private static EndlessPhaseCardSO CreatePhaseCard(string phaseId, float duration)
    {
        EndlessPhaseCardSO card = ScriptableObject.CreateInstance<EndlessPhaseCardSO>();
        SetField(card, "phaseId", phaseId);
        SetField(card, "displayName", phaseId);
        SetField(card, "duration", duration);
        SetField(card, "completionMode", WaveCompletionMode.TimerOnly);
        SetField(card, "totalBudget", 10f);
        SetField(card, "alivePressureCap", 10f);
        SetField(card, "pacingCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
        SetField(card, "compositionTargets", new[] { CreateRoleTarget(SpawnRole.Melee, 1f) });
        SetField(card, "roster", new[] { CreateRosterEntry($"{phaseId}_enemy", SpawnRole.Melee) });
        SetField(card, "scriptedBeats", System.Array.Empty<ScriptedSpawnBeat>());
        return card;
    }

    private static SpawnRoleTarget CreateRoleTarget(SpawnRole role, float budgetShare)
    {
        SpawnRoleTarget target = new SpawnRoleTarget();
        SetField(ref target, "role", role);
        SetField(ref target, "budgetShare", budgetShare);
        SetField(ref target, "minBudget", 0f);
        SetField(ref target, "maxBudget", 0f);
        SetField(ref target, "priority", 0);
        return target;
    }

    private static EnemyRosterEntry CreateRosterEntry(string entryId, SpawnRole role)
    {
        EnemyRosterEntry entry = new EnemyRosterEntry();
        SetField(entry, "entryId", entryId);
        SetField(entry, "enemy", CreateEnemy(entryId));
        SetField(entry, "role", role);
        SetField(entry, "tags", WaveEnemyTag.Normal);
        SetField(entry, "cost", 1f);
        SetField(entry, "minGroupSize", 1);
        SetField(entry, "maxGroupSize", 1);
        SetField(entry, "cooldownSeconds", 0f);
        SetField(entry, "maxAlive", 0);
        SetField(entry, "activeTimeRange", new Vector2(0f, 100f));
        return entry;
    }

    private static EnemySO CreateEnemy(string name)
    {
        SkeletonEnemySO enemy = ScriptableObject.CreateInstance<SkeletonEnemySO>();
        enemy.name = name;
        return enemy;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void SetField<TStruct>(ref TStruct target, string fieldName, object value)
        where TStruct : struct
    {
        object boxed = target;
        SetField(boxed, fieldName, value);
        target = (TStruct)boxed;
    }
}
