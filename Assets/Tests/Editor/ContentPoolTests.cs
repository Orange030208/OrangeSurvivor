using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class ContentPoolTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        ContentPoolModifierRegistry.ClearForTests();
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void CurrentWaveConditionFiltersCandidates()
    {
        ContentPoolEntry earlyEntry = CreateEntry("early", 1f);
        ContentPoolEntry lateEntry = CreateEntry("late", 1f);
        lateEntry.ConfigureRuntimeRules(
            new ContentCondition[]
            {
                new CurrentWaveCondition(
                    ContentComparisonOperator.GreaterOrEqual,
                    3)
            },
            null);
        ContentPoolSO pool = CreatePool(
            new[] { earlyEntry, lateEntry },
            4,
            true);
        ContentRollContext context = new(
            ContentPoolScopeIds.Generic,
            progressionSnapshot: new RunProgressionSnapshot(2, 20, 0f, 0, 1f, 1f, 1f, 0));

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(10))
            .Roll(pool, context, 4);

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
            new[] { zeroEntry, weightedEntry },
            8,
            true);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(20))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic), 8);

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
            new[] { CreateEntry("a", 1f), CreateEntry("b", 2f), CreateEntry("c", 3f) },
            12,
            true);

        ContentRollResult first = new ContentPoolRollService(new SystemContentRandom(1234))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic), 12);
        ContentRollResult second = new ContentPoolRollService(new SystemContentRandom(1234))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic), 12);

        Assert.AreEqual(first.Items.Count, second.Items.Count);
        for (int i = 0; i < first.Items.Count; i++)
        {
            Assert.AreEqual(first.Items[i].EntryId, second.Items[i].EntryId);
        }
    }

    [Test]
    public void PlayerPropertyScaleWeightRuleCanUseLowerBoundWithoutUpperBound()
    {
        TestAccessoryEntity owner = CreateAccessoryOwner("luck_weight_owner");
        PropertiesManager propertiesManager = owner.GetComponent<PropertiesManager>();
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        createdObjects.Add(target);
        ContentPoolEntry entry = new(target, 10f, "target");
        entry.ConfigureRuntimeRules(
            null,
            new ContentWeightRule[]
            {
                new PlayerPropertyScaleWeightRule(PropType.Luck, 0.01f, 0.5f, 0f)
            });
        ContentPoolSO pool = CreatePool(new[] { entry }, 1, false);

        propertiesManager.AddModifier("positive_luck", new PropModifierData(PropType.Luck, 1000f));
        ContentRollResult positiveResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic, source: owner), 1);

        propertiesManager.RemoveModifiers("positive_luck");
        propertiesManager.AddModifier("negative_luck", new PropModifierData(PropType.Luck, -1000f));
        ContentRollResult negativeResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic, source: owner), 1);

        Assert.AreEqual(110f, positiveResult.Items[0].FinalWeight);
        Assert.AreEqual(5f, negativeResult.Items[0].FinalWeight);
    }

    [Test]
    public void PlayerPropertyScaleWeightRuleCanReduceLowTierWeightToLowerBound()
    {
        TestAccessoryEntity owner = CreateAccessoryOwner("negative_luck_weight_owner");
        PropertiesManager propertiesManager = owner.GetComponent<PropertiesManager>();
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        createdObjects.Add(target);
        ContentPoolEntry entry = new(target, 10f, "target");
        entry.ConfigureRuntimeRules(
            null,
            new ContentWeightRule[]
            {
                new PlayerPropertyScaleWeightRule(PropType.Luck, -0.4f / 250f, 0.5f, 0f)
            });
        ContentPoolSO pool = CreatePool(new[] { entry }, 1, false);

        propertiesManager.AddModifier("high_luck", new PropModifierData(PropType.Luck, 1000f));
        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic, source: owner), 1);

        Assert.AreEqual(5f, result.Items[0].FinalWeight);
    }

    [Test]
    public void RegisteredModifierAffectsPoolUntilUnregistered()
    {
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        ContentPoolEntry targetEntry = new(target, 1f, "target");
        ContentPoolEntry otherEntry = CreateEntry("other", 1f);
        ContentPoolSO pool = CreatePool(
            new[] { targetEntry, otherEntry },
            1,
            false);
        TestAssetWeightModifier modifier = new(target, -1f);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult modifiedResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic), 1);

        ContentPoolModifierRegistry.Unregister(modifier);
        ContentRollResult restoredResult = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Generic), 1);

        Assert.AreSame(otherEntry.Content, modifiedResult.Items[0].Content);
        Assert.AreSame(targetEntry.Content, restoredResult.Items[0].Content);
    }

    [Test]
    public void RegisteredModifierCanOverrideRolledMetadata()
    {
        ContentPoolEntry entry = CreateEntry("priced", 1f);
        entry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WeaponLevelRollMetadata(1, 4),
            new ShopPricingMetadata(1f)
        });
        ContentPoolSO pool = CreatePool(
            new[] { entry },
            1,
            false);
        TestMetadataModifier modifier = new(entry.Content, 2, 3, 0.5f);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.Shop), 1);

        Assert.IsTrue(result.Items[0].TryGetMetadata(out WeaponLevelRollMetadata levelMetadata));
        Assert.AreEqual(2, levelMetadata.MinLevel);
        Assert.AreEqual(3, levelMetadata.MaxLevel);
        Assert.IsTrue(result.Items[0].TryGetMetadata(out ShopPricingMetadata pricingMetadata));
        Assert.AreEqual(0.5f, pricingMetadata.PriceMultiplier);
        Assert.IsTrue(entry.TryGetMetadata(out WeaponLevelRollMetadata entryLevelMetadata));
        Assert.AreEqual(1, entryLevelMetadata.MinLevel);
        Assert.AreEqual(4, entryLevelMetadata.MaxLevel);
    }

    [Test]
    public void MaxPickCountFiltersPreviouslyPickedEntry()
    {
        ContentPoolEntry limitedEntry = CreateEntry("limited", 100f);
        limitedEntry.ConfigureRuntimeLimits(0, 1);
        ContentPoolEntry fallbackEntry = CreateEntry("fallback", 1f);
        ContentPoolSO pool = CreatePool(
            new[] { limitedEntry, fallbackEntry },
            1,
            false);
        ContentHistoryState history = new();
        ContentHistoryScope scope = new(ContentPoolScopeIds.UpgradeCard, "upgrade_pool", "player");
        history.RecordPick(scope, new ContentRollItem(limitedEntry, limitedEntry.Content, 1f));
        ContentRollContext context = new(
            ContentPoolScopeIds.UpgradeCard,
            historyScope: scope,
            history: history);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, context, 1);

        Assert.AreSame(fallbackEntry.Content, result.Items[0].Content);
    }

    [Test]
    public void ContentHistoryStateScopesRollAndPickCounts()
    {
        ContentHistoryState history = new();
        ContentHistoryScope upgradeScope = new(ContentPoolScopeIds.UpgradeCard, "upgrade_pool", "player");
        ContentHistoryScope shopScope = new(ContentPoolScopeIds.Shop, "shop_pool", "player");
        ContentPoolEntry entry = CreateEntry("shared_entry", 1f);
        ContentRollItem item = new(entry, entry.Content, 1f);

        history.RecordRoll(upgradeScope, new[] { item });
        history.RecordPick(upgradeScope, item);

        Assert.AreEqual(1, history.GetRollCount(upgradeScope, "shared_entry"));
        Assert.AreEqual(1, history.GetPickCount(upgradeScope, "shared_entry"));
        Assert.IsTrue(history.WasPreviouslyRolled(upgradeScope, "shared_entry"));
        Assert.IsTrue(history.WasPreviouslyOffered(upgradeScope, "shared_entry"));

        Assert.AreEqual(0, history.GetRollCount(shopScope, "shared_entry"));
        Assert.AreEqual(0, history.GetPickCount(shopScope, "shared_entry"));
        Assert.IsFalse(history.WasPreviouslyRolled(shopScope, "shared_entry"));
        Assert.IsFalse(history.WasPreviouslyOffered(shopScope, "shared_entry"));
    }

    [Test]
    public void ContentHistoryStateRecordsAllUpgradeCardTagBits()
    {
        ContentHistoryState history = new();
        ContentHistoryScope scope = new(ContentPoolScopeIds.UpgradeCard, "upgrade_pool", "player");
        UpgradeCardSO card = ScriptableObject.CreateInstance<UpgradeCardSO>();
        card.InitializeRuntime(
            "multi_tag",
            "Multi Tag",
            UpgradeCardRarity.Common,
            new[] { UpgradeCardTag.Attack, UpgradeCardTag.Weapon, UpgradeCardTag.Ranged },
            string.Empty);
        ContentPoolEntry entry = new(card, 1f, card.CardId);
        ContentRollItem item = new(entry, card, 1f);

        history.RecordPick(scope, item);

        Assert.AreEqual(1, history.GetUpgradeCardTagPickCount(scope, UpgradeCardTag.Attack));
        Assert.AreEqual(1, history.GetUpgradeCardTagPickCount(scope, UpgradeCardTag.Weapon));
        Assert.AreEqual(1, history.GetUpgradeCardTagPickCount(scope, UpgradeCardTag.Ranged));
        Assert.AreEqual(1, history.GetUpgradeCardTagPickCount(
            scope,
            UpgradeCardTag.Attack | UpgradeCardTag.Weapon,
            ContentTagMatchMode.All));
        Assert.AreEqual(1, history.GetUpgradeCardTagPickCount(
            scope,
            UpgradeCardTag.Attack | UpgradeCardTag.Weapon | UpgradeCardTag.Ranged,
            ContentTagMatchMode.Exact));
        Assert.AreEqual(0, history.GetUpgradeCardTagPickCount(scope, UpgradeCardTag.Defense));
    }

    [Test]
    public void RollWithContentRollContextUsesHistoryForMaxPickCount()
    {
        ContentPoolEntry limitedEntry = CreateEntry("limited", 100f);
        limitedEntry.ConfigureRuntimeLimits(0, 1);
        ContentPoolEntry fallbackEntry = CreateEntry("fallback", 1f);
        ContentPoolSO pool = CreatePool(
            new[] { limitedEntry, fallbackEntry },
            1,
            false);
        ContentHistoryState history = new();
        ContentHistoryScope scope = new(ContentPoolScopeIds.UpgradeCard, "upgrade_pool", "player");
        history.RecordPick(scope, new ContentRollItem(limitedEntry, limitedEntry.Content, 1f));
        ContentRollContext context = new(
            ContentPoolScopeIds.UpgradeCard,
            historyScope: scope,
            history: history);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, context, 1);

        Assert.AreSame(fallbackEntry.Content, result.Items[0].Content);
    }

    [Test]
    public void MutualExclusionPreventsSameRollSelection()
    {
        ContentPoolEntry left = CreateEntry("left", 100f);
        left.ConfigureRuntimeMutuallyExclusiveEntries(new[] { "right" });
        ContentPoolEntry right = CreateEntry("right", 100f);
        ContentPoolSO pool = CreatePool(
            new[] { left, right },
            2,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.UpgradeCard), 2);

        Assert.AreEqual(1, result.Items.Count);
        Assert.IsTrue(result.Items[0].EntryId == "left" || result.Items[0].EntryId == "right");
    }

    [Test]
    public void UniqueUpgradeCardTagConditionPreventsSameTagInSingleRoll()
    {
        UpgradeCardSO firstCard = CreateUpgradeCard("first", UpgradeCardTag.Attack);
        UpgradeCardSO secondCard = CreateUpgradeCard("second", UpgradeCardTag.Attack);
        ContentPoolEntry firstEntry = new(firstCard, 100f, firstCard.CardId);
        firstEntry.ConfigureRuntimeRules(
            new ContentCondition[] { new UniqueUpgradeCardTagCondition(UpgradeCardTag.Attack) },
            null);
        ContentPoolEntry secondEntry = new(secondCard, 100f, secondCard.CardId);
        secondEntry.ConfigureRuntimeRules(
            new ContentCondition[] { new UniqueUpgradeCardTagCondition(UpgradeCardTag.Attack) },
            null);
        ContentPoolSO pool = CreatePool(
            new[] { firstEntry, secondEntry },
            2,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.UpgradeCard), 2);

        Assert.AreEqual(1, result.Items.Count);
    }

    [Test]
    public void AccessoryOwnedLimitConditionFiltersAlreadyOwnedAccessory()
    {
        TestAccessoryEntity owner = CreateAccessoryOwner("accessory_owner");
        AccessoryManager accessoryManager = owner.GetComponent<AccessoryManager>();
        AccessoryDataSO limitedAccessory = CreateAccessory("limited_accessory", 1);
        AccessoryDataSO fallbackAccessory = CreateAccessory("fallback_accessory", 0);
        ContentPoolEntry limitedEntry = CreateAccessoryEntry(limitedAccessory, 100f);
        ContentPoolEntry fallbackEntry = CreateAccessoryEntry(fallbackAccessory, 1f);
        ContentPoolSO pool = CreatePool(
            new[] { limitedEntry, fallbackEntry },
            1,
            false);

        Assert.IsTrue(accessoryManager.EquipAccessory(limitedAccessory, false));

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(
                pool,
                new ContentRollContext(ContentPoolScopeIds.ChestReward, source: owner),
                1);

        Assert.AreEqual(1, result.Items.Count);
        Assert.AreSame(fallbackAccessory, result.Items[0].Content);
    }

    [Test]
    public void AccessoryOwnedLimitConditionPreventsSameLimitedAccessoryInSingleRoll()
    {
        AccessoryDataSO accessory = CreateAccessory("single_roll_unique_accessory", 1);
        ContentPoolEntry entry = CreateAccessoryEntry(accessory, 1f);
        ContentPoolSO pool = CreatePool(new[] { entry }, 2, true);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(
                pool,
                new ContentRollContext(ContentPoolScopeIds.ChestReward),
                2);

        Assert.AreEqual(1, result.Items.Count);
    }

    [Test]
    public void AccessoryOwnedLimitConditionAllowsUnlimitedAccessoryDuplicates()
    {
        AccessoryDataSO accessory = CreateAccessory("unlimited_accessory", 0);
        ContentPoolEntry entry = CreateAccessoryEntry(accessory, 1f);
        ContentPoolSO pool = CreatePool(new[] { entry }, 2, true);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(
                pool,
                new ContentRollContext(ContentPoolScopeIds.ChestReward),
                2);

        Assert.AreEqual(2, result.Items.Count);
    }

    [Test]
    public void AccessoryManagerRejectsEquipWhenOwnedLimitReached()
    {
        TestAccessoryEntity owner = CreateAccessoryOwner("accessory_limit_owner");
        AccessoryManager accessoryManager = owner.GetComponent<AccessoryManager>();
        PropertiesManager propertiesManager = owner.GetComponent<PropertiesManager>();
        AccessoryDataSO accessory = CreateAccessory(
            "limited_stat_accessory",
            1,
            new[] { new PropModifierData(PropType.MaxHealth, 10f) });

        Assert.IsTrue(accessoryManager.EquipAccessory(accessory, false));
        Assert.AreEqual(1, accessoryManager.GetEquippedCount(accessory));
        Assert.IsFalse(accessoryManager.CanEquipAccessory(accessory));
        Assert.AreEqual(10f, propertiesManager.GetPropValue(PropType.MaxHealth));

        Assert.IsFalse(accessoryManager.EquipAccessory(accessory, false));

        Assert.AreEqual(1, accessoryManager.GetEquippedCount(accessory));
        Assert.AreEqual(1, accessoryManager.EquippedAccessoryList.Count);
        Assert.AreEqual(10f, propertiesManager.GetPropValue(PropType.MaxHealth));
    }

    [Test]
    public void WaveSpawnScopeUsesContentPoolModifiers()
    {
        Object target = ScriptableObject.CreateInstance<ContentPoolSO>();
        ContentPoolEntry targetEntry = new(target, 1f, "target");
        ContentPoolEntry otherEntry = CreateEntry("other_wave", 1f);
        TestScopeWeightModifier modifier = new(ContentPoolScopeIds.WaveSpawn, target, -1f);
        ContentPoolSO pool = CreatePool(
            new[] { targetEntry, otherEntry },
            1,
            false);

        ContentPoolModifierRegistry.Register(modifier);
        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.WaveSpawn), 1);

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
            new[] { packEntry },
            1,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(
                pool,
                new ContentRollContext(ContentPoolScopeIds.WaveSpawn),
                1,
                entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);

        Assert.IsTrue(result.HasAny);
        Assert.AreSame(spawnPack, result.Items[0].Content);
        Assert.AreEqual(2, spawnPack.Entries.Count);
        Assert.AreEqual(3, spawnPack.Entries[0].SpawnCount);
        Assert.AreEqual(WaveEnemyTag.Ranged, spawnPack.Entries[1].EnemyTags);
    }

    [Test]
    public void RollItemCarriesTypedWaveSpawnMetadata()
    {
        WormEnemySO enemy = ScriptableObject.CreateInstance<WormEnemySO>();
        ContentPoolEntry enemyEntry = new(enemy, 1f, "elite_ranged");
        enemyEntry.ConfigureRuntimeMetadata(new ContentEntryMetadata[]
        {
            new WaveSpawnMetadata(WaveEnemyTag.Elite | WaveEnemyTag.Ranged)
        });
        ContentPoolSO pool = CreatePool(
            new[] { enemyEntry },
            1,
            false);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, new ContentRollContext(ContentPoolScopeIds.WaveSpawn), 1);

        Assert.IsTrue(result.HasAny);
        Assert.IsTrue(result.Items[0].TryGetMetadata(out WaveSpawnMetadata metadata));
        Assert.AreEqual(WaveEnemyTag.Elite | WaveEnemyTag.Ranged, metadata.Tags);
    }

    private static ContentPoolEntry CreateEntry(string entryId, float weight)
    {
        Object content = ScriptableObject.CreateInstance<ContentPoolSO>();
        return new ContentPoolEntry(content, weight, entryId);
    }

    private static UpgradeCardSO CreateUpgradeCard(string cardId, UpgradeCardTag tag)
    {
        UpgradeCardSO card = ScriptableObject.CreateInstance<UpgradeCardSO>();
        card.InitializeRuntime(
            cardId,
            cardId,
            UpgradeCardRarity.Common,
            new[] { tag },
            string.Empty);
        return card;
    }

    private AccessoryDataSO CreateAccessory(
        string accessoryId,
        int maxOwnedCount,
        IReadOnlyList<PropModifierData> propertyModifiers = null)
    {
        AccessoryDataSO accessory = ScriptableObject.CreateInstance<AccessoryDataSO>();
        accessory.name = accessoryId;
        createdObjects.Add(accessory);
        SetPrivateField(accessory, "accessoryId", accessoryId);
        SetPrivateField(accessory, "itemName", accessoryId);
        SetPrivateField(accessory, "itemType", ItemType.Accessory);
        SetPrivateField(accessory, "maxOwnedCount", maxOwnedCount);
        if (propertyModifiers != null)
        {
            SetPrivateField(accessory, "propertyModifiers", new List<PropModifierData>(propertyModifiers));
        }

        return accessory;
    }

    private static ContentPoolEntry CreateAccessoryEntry(AccessoryDataSO accessory, float weight)
    {
        ContentPoolEntry entry = new(accessory, weight, accessory.AccessoryId);
        entry.ConfigureRuntimeRules(new ContentCondition[] { new AccessoryOwnedLimitCondition() }, null);
        return entry;
    }

    private TestAccessoryEntity CreateAccessoryOwner(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        TestAccessoryEntity entity = gameObject.AddComponent<TestAccessoryEntity>();
        PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();
        FeatureHost featureHost = gameObject.AddComponent<FeatureHost>();
        AccessoryManager accessoryManager = gameObject.AddComponent<AccessoryManager>();

        propertiesManager.Initialize(entity);
        featureHost.Initialize(entity);
        accessoryManager.Initialize(entity);
        return entity;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (Type type = target.GetType(); type != null && field == null; type = type.BaseType)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static ContentPoolSO CreatePool(
        IReadOnlyList<ContentPoolEntry> entries,
        int rollCount,
        bool allowDuplicateResults)
    {
        ContentPoolSO pool = ScriptableObject.CreateInstance<ContentPoolSO>();
        pool.Initialize(entries, rollCount, allowDuplicateResults);
        return pool;
    }

    private sealed class TestAccessoryEntity : Entity, IFeatureEffectsProvider
    {
        public IReadOnlyList<FeatureEffectBase> FeatureEffects => Array.Empty<FeatureEffectBase>();
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

        public bool AffectsContext(ContentRollContext context)
        {
            return true;
        }

        public void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
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

        public bool AffectsContext(ContentRollContext context)
        {
            return context != null &&
                   string.Equals(context.ScopeId, ContentPoolScopeIds.Shop, System.StringComparison.Ordinal);
        }

        public void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
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

    private sealed class TestScopeWeightModifier : IContentPoolModifier
    {
        private readonly string scopeId;
        private readonly Object target;
        private readonly float addedWeight;

        public TestScopeWeightModifier(string scopeId, Object target, float addedWeight)
        {
            this.scopeId = ContentPoolScopeIds.Normalize(scopeId);
            this.target = target;
            this.addedWeight = addedWeight;
        }

        public int Priority => 0;

        public bool AffectsContext(ContentRollContext context)
        {
            return context != null &&
                   string.Equals(scopeId, context.ScopeId, System.StringComparison.Ordinal);
        }

        public void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
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
