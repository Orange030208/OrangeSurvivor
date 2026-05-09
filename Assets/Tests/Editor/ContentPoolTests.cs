using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ContentPoolTests
{
    [TearDown]
    public void TearDown()
    {
        ContentPoolModifierRegistry.ClearForTests();
    }

    [Test]
    public void FactSetStoresAndReadsTypedValues()
    {
        FactDefinitionSO fact = CreateFact("test_int", FactValueType.Int);
        ContentFactSet facts = new();

        facts.Set(fact, ContentFactValue.FromInt(7));

        Assert.IsTrue(facts.TryGet(fact, out ContentFactValue value));
        Assert.AreEqual(FactValueType.Int, value.ValueType);
        Assert.AreEqual(7, value.IntValue);
    }

    [Test]
    public void FactCompareConditionFiltersCandidates()
    {
        FactDefinitionSO waveFact = CreateFact(
            "current_wave",
            FactValueType.Int,
            FactDefinitionBuiltInKind.CurrentWave);
        ContentPoolEntry earlyEntry = CreateEntry("early", 1f);
        ContentPoolEntry lateEntry = CreateEntry("late", 1f);
        lateEntry.ConfigureRuntimeRules(
            new ContentCondition[]
            {
                new FactCompareContentCondition(
                    waveFact,
                    ContentFactComparisonOperator.GreaterOrEqual,
                    ContentFactValue.FromInt(3))
            },
            null);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.Generic,
            new[] { earlyEntry, lateEntry },
            4,
            true);
        ContentFactSet facts = ContentFactCollector.Collect(
            new ContentFactSource { WaveNumber = 2 },
            new[] { waveFact });

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(10))
            .Roll(pool, facts, null, 4);

        Assert.AreEqual(4, result.Items.Count);
        for (int i = 0; i < result.Items.Count; i++)
        {
            Assert.AreSame(earlyEntry.Content, result.Items[i].Content);
        }
    }

    [Test]
    public void ZeroWeightCandidatesAreNotRolled()
    {
        ContentPoolEntry zeroEntry = CreateEntry("zero", 0f);
        ContentPoolEntry weightedEntry = CreateEntry("weighted", 1f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.Generic,
            new[] { zeroEntry, weightedEntry },
            8,
            true);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(20))
            .Roll(pool, ContentFactSet.Empty, null, 8);

        Assert.AreEqual(8, result.Items.Count);
        for (int i = 0; i < result.Items.Count; i++)
        {
            Assert.AreSame(weightedEntry.Content, result.Items[i].Content);
        }
    }

    [Test]
    public void SameSeedProducesSameRollSequence()
    {
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.Generic,
            new[] { CreateEntry("a", 1f), CreateEntry("b", 2f), CreateEntry("c", 3f) },
            12,
            true);

        ContentRollResult first = new ContentPoolRollService(new SystemContentRandom(1234))
            .Roll(pool, ContentFactSet.Empty, null, 12);
        ContentRollResult second = new ContentPoolRollService(new SystemContentRandom(1234))
            .Roll(pool, ContentFactSet.Empty, null, 12);

        Assert.AreEqual(first.Items.Count, second.Items.Count);
        for (int i = 0; i < first.Items.Count; i++)
        {
            Assert.AreEqual(first.Items[i].EntryId, second.Items[i].EntryId);
        }
    }

    [Test]
    public void RegisteredModifierAffectsPoolUntilUnregistered()
    {
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        ContentPoolEntry targetEntry = new(target, 1f, "target");
        ContentPoolEntry otherEntry = CreateEntry("other", 1f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.Generic,
            new[] { targetEntry, otherEntry },
            1,
            false);
        TestAssetWeightModifier modifier = new(target, -1f);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult modifiedResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 1);

        ContentPoolModifierRegistry.Unregister(modifier);
        ContentRollResult restoredResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 1);

        Assert.AreSame(otherEntry.Content, modifiedResult.Items[0].Content);
        Assert.AreSame(targetEntry.Content, restoredResult.Items[0].Content);
    }

    [Test]
    public void RegisteredModifierCanOverrideRolledMetadata()
    {
        ContentPoolEntry entry = CreateEntry("priced", 1f);
        entry.ConfigureRuntimeMetadata(1, 4, 0, 1f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.Shop,
            new[] { entry },
            1,
            false);
        TestMetadataModifier modifier = new(entry.Content, 2, 3, 0.5f);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 1);

        Assert.AreEqual(2, result.Items[0].MinLevel);
        Assert.AreEqual(3, result.Items[0].MaxLevel);
        Assert.AreEqual(0.5f, result.Items[0].PriceMultiplier);
    }

    [Test]
    public void MaxPickCountFiltersPreviouslyPickedEntry()
    {
        ContentPoolEntry limitedEntry = CreateEntry("limited", 100f);
        limitedEntry.ConfigureRuntimeLimits(0, 1, null);
        ContentPoolEntry fallbackEntry = CreateEntry("fallback", 1f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.UpgradeCard,
            new[] { limitedEntry, fallbackEntry },
            1,
            false);
        ContentPoolRuntimeState runtimeState = new();
        runtimeState.RecordPick("limited");

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, runtimeState, 1);

        Assert.AreSame(fallbackEntry.Content, result.Items[0].Content);
    }

    [Test]
    public void MutualExclusionPreventsSameRollSelection()
    {
        ContentPoolEntry left = CreateEntry("left", 100f);
        left.ConfigureRuntimeLimits(0, 0, new[] { "right" });
        ContentPoolEntry right = CreateEntry("right", 100f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.UpgradeCard,
            new[] { left, right },
            2,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 2);

        Assert.AreEqual(1, result.Items.Count);
        Assert.IsTrue(result.Items[0].EntryId == "left" || result.Items[0].EntryId == "right");
    }

    [Test]
    public void WaveSpawnPurposeUsesContentPoolModifiers()
    {
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        ContentPoolEntry targetEntry = new(target, 1f, "target");
        ContentPoolEntry otherEntry = CreateEntry("other_wave", 1f);
        TestPurposeWeightModifier modifier = new(ContentPoolPurpose.WaveSpawn, target, -1f);
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.WaveSpawn,
            new[] { targetEntry, otherEntry },
            1,
            false);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 1);

        Assert.AreSame(otherEntry.Content, result.Items[0].Content);
    }

    [Test]
    public void WaveSpawnPoolCanRollSpawnPackContent()
    {
        WormEnemySO meleeEnemy = ScriptableObject.CreateInstance<WormEnemySO>();
        FlyForestEnemySO rangedEnemy = ScriptableObject.CreateInstance<FlyForestEnemySO>();
        WaveSpawnPackSO spawnPack = ScriptableObject.CreateInstance<WaveSpawnPackSO>();
        spawnPack.InitializeRuntime(
            "ambush_pack",
            new[]
            {
                new WaveSpawnPackEntry(meleeEnemy, 3, WaveEnemyTag.Normal),
                new WaveSpawnPackEntry(rangedEnemy, 1, WaveEnemyTag.Ranged)
            });
        ContentPoolEntry packEntry = new(spawnPack, 1f, "ambush_pack");
        ContentPoolSO pool = CreatePool(
            ContentPoolPurpose.WaveSpawn,
            new[] { packEntry },
            1,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, ContentFactSet.Empty, null, 1, entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);

        Assert.IsTrue(result.HasAny);
        Assert.AreSame(spawnPack, result.Items[0].Content);
        Assert.AreEqual(2, spawnPack.Entries.Count);
        Assert.AreEqual(3, spawnPack.Entries[0].SpawnCount);
        Assert.AreEqual(WaveEnemyTag.Ranged, spawnPack.Entries[1].EnemyTags);
    }

    private static FactDefinitionSO CreateFact(
        string factId,
        FactValueType valueType,
        FactDefinitionBuiltInKind builtInKind = FactDefinitionBuiltInKind.None)
    {
        FactDefinitionSO fact = ScriptableObject.CreateInstance<FactDefinitionSO>();
        fact.InitializeRuntime(factId, valueType, builtInKind);
        return fact;
    }

    private static ContentPoolEntry CreateEntry(string entryId, float weight)
    {
        Object content = ScriptableObject.CreateInstance<ContentPoolSO>();
        return new ContentPoolEntry(content, weight, entryId);
    }

    private static ContentPoolSO CreatePool(
        ContentPoolPurpose purpose,
        IReadOnlyList<ContentPoolEntry> entries,
        int rollCount,
        bool allowDuplicateResults)
    {
        ContentPoolSO pool = ScriptableObject.CreateInstance<ContentPoolSO>();
        pool.Initialize(purpose, entries, rollCount, allowDuplicateResults);
        return pool;
    }

    private sealed class TestAssetWeightModifier : IContentPoolModifier
    {
        private readonly Object target;
        private readonly float addedWeight;

        public TestAssetWeightModifier(Object target, float addedWeight)
        {
            this.target = target;
            this.addedWeight = addedWeight;
        }

        public int Priority => 0;

        public bool AffectsPurpose(ContentPoolPurpose purpose)
        {
            return true;
        }

        public void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                ContentPoolCandidate candidate = candidates[i];
                if (candidate.Content == target)
                {
                    candidate.Weight = Mathf.Max(0f, candidate.Weight + addedWeight);
                }
            }
        }
    }

    private sealed class TestMetadataModifier : IContentPoolModifier
    {
        private readonly Object target;
        private readonly int minLevel;
        private readonly int maxLevel;
        private readonly float priceMultiplier;

        public TestMetadataModifier(Object target, int minLevel, int maxLevel, float priceMultiplier)
        {
            this.target = target;
            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
            this.priceMultiplier = priceMultiplier;
        }

        public int Priority => 0;

        public bool AffectsPurpose(ContentPoolPurpose purpose)
        {
            return purpose == ContentPoolPurpose.Shop;
        }

        public void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                ContentPoolCandidate candidate = candidates[i];
                if (candidate.Content != target)
                {
                    continue;
                }

                candidate.ConfigureLevelRange(minLevel, maxLevel);
                candidate.ConfigurePriceMultiplier(priceMultiplier);
            }
        }
    }

    private sealed class TestPurposeWeightModifier : IContentPoolModifier
    {
        private readonly ContentPoolPurpose purpose;
        private readonly Object target;
        private readonly float addedWeight;

        public TestPurposeWeightModifier(ContentPoolPurpose purpose, Object target, float addedWeight)
        {
            this.purpose = purpose;
            this.target = target;
            this.addedWeight = addedWeight;
        }

        public int Priority => 0;

        public bool AffectsPurpose(ContentPoolPurpose purpose)
        {
            return this.purpose == purpose;
        }

        public void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                ContentPoolCandidate candidate = candidates[i];
                if (candidate.Content == target)
                {
                    candidate.Weight = Mathf.Max(0f, candidate.Weight + addedWeight);
                }
            }
        }
    }
}
