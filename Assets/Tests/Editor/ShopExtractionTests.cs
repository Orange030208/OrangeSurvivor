using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Orange.Extraction;
using UnityEditor;
using UnityEngine;

public sealed class ShopExtractionTests
{
    [Test]
    public void ContentTierWeightProfile_ReturnsConfiguredNonNegativeWeights()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(1f, -2f, 3f, 4f);

        Assert.That(profile.GetWeight(ContentTier.Common), Is.EqualTo(1f));
        Assert.That(profile.GetWeight(ContentTier.Rare), Is.EqualTo(0f));
        Assert.That(profile.GetWeight(ContentTier.Epic), Is.EqualTo(3f));
        Assert.That(profile.GetWeight(ContentTier.Legendary), Is.EqualTo(4f));
    }

    [Test]
    public void ShopExtraction_UsesSameTierWeightForAccessoryAndMatchingWeaponLevel()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(1f, 5f, 7f, 9f);
        AccessoryDataSO rareAccessory = CreateAccessory("rare_accessory", ContentTier.Rare);
        WeaponDataSO weapon = CreateWeapon("weapon");
        ShopExtractionRoller roller = new();

        ShopExtractionPool pool = roller.CreatePool(
            new[] { weapon },
            new[] { rareAccessory },
            profile);

        var evaluation = pool.Evaluate(new ShopExtractionContext(null));

        Assert.That(FindCandidate(evaluation.Candidates, "rare_accessory").FinalWeight, Is.EqualTo(5f));
        Assert.That(FindCandidate(evaluation.Candidates, "weapon_Lv2").FinalWeight, Is.EqualTo(5f));
    }

    [Test]
    public void ShopExtraction_UsesDeterministicRandomSelection()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(0f, 0f, 0f, 1f);
        WeaponDataSO weapon = CreateWeapon("weapon");
        ShopExtractionRoller roller = new(new SequenceExtractionRandom(0f));

        bool wasRolled = roller.TryRollOne(
            new[] { weapon },
            Array.Empty<AccessoryDataSO>(),
            profile,
            new ShopExtractionContext(null),
            out ShopExtractionCandidate candidate);

        Assert.That(wasRolled, Is.True);
        Assert.That(candidate.EntryId, Is.EqualTo("weapon_Lv4"));
        Assert.That(candidate.Level, Is.EqualTo(WeaponLevelHelper.MaxLevel));
        Assert.That(candidate.Tier, Is.EqualTo(ContentTier.Legendary));
    }

    [Test]
    public void ShopExtraction_ReturnsFalseWhenAllTierWeightsAreZero()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(0f, 0f, 0f, 0f);
        ShopExtractionRoller roller = new(new SequenceExtractionRandom(0f));

        bool wasRolled = roller.TryRollOne(
            new[] { CreateWeapon("weapon") },
            new[] { CreateAccessory("accessory", ContentTier.Common) },
            profile,
            new ShopExtractionContext(null),
            out ShopExtractionCandidate candidate);

        Assert.That(wasRolled, Is.False);
        Assert.That(candidate, Is.Null);
    }

    [Test]
    public void ShopItemData_GetPrice_UsesRunAndPlayerMultipliersOnly()
    {
        WeaponDataSO weapon = CreateWeapon("priced_weapon");
        ShopItemData item = new()
        {
            ItemData = weapon,
            Level = WeaponLevelHelper.MinLevel,
            Lock = false,
            SoldOut = false,
            RunPriceMultiplier = 1.5f,
            PlayerDiscountMultiplier = 0.8f
        };

        Assert.That(item.GetPrice(), Is.EqualTo(12));
    }

    [Test]
    public void ShopExtraction_FiltersAccessoriesAtOwnedLimit()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(1f, 0f, 0f, 0f);
        AccessoryDataSO blockedAccessory = CreateAccessory("blocked_accessory", ContentTier.Common, maxOwnedCount: 1);
        AccessoryManager accessoryManager = CreateAccessoryManagerWithEquipped(blockedAccessory);
        ShopExtractionRoller roller = new();

        ShopExtractionPool pool = roller.CreatePool(
            Array.Empty<WeaponDataSO>(),
            new[] { blockedAccessory },
            profile);

        var evaluation = pool.Evaluate(new ShopExtractionContext(accessoryManager));

        Assert.That(evaluation.HasDrawableCandidates, Is.False);
        Assert.That(evaluation.Candidates.Count, Is.EqualTo(1));
        Assert.That(evaluation.Candidates[0].Status, Is.EqualTo(ExtractionCandidateStatus.Ineligible));
        UnityEngine.Object.DestroyImmediate(accessoryManager.gameObject);
    }

    [Test]
    public void ShopExtraction_DrawManyUniqueDoesNotReturnDuplicateItemLevelPairs()
    {
        ContentTierWeightProfileSO profile = CreateTierWeightProfile(1f, 1f, 1f, 1f);
        WeaponDataSO weapon = CreateWeapon("weapon");
        AccessoryDataSO accessory = CreateAccessory("accessory", ContentTier.Common);
        ShopExtractionRoller roller = new(new SequenceExtractionRandom(0f, 0f, 0f));

        IReadOnlyList<ShopExtractionCandidate> candidates = roller.DrawManyUnique(
            new[] { weapon },
            new[] { accessory },
            profile,
            new ShopExtractionContext(null),
            3);

        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int i = 0; i < candidates.Count; i++)
        {
            Assert.That(seen.Add($"{candidates[i].ItemData.GetInstanceID()}_{candidates[i].Level}"), Is.True);
        }
    }

    [Test]
    public void ShopManager_RefreshKeepingLockedItemsPreservesLockedEntries()
    {
        GameObject gameObject = new("ShopManager Test Host");
        ShopManager manager = gameObject.AddComponent<ShopManager>();
        WeaponDataSO lockedWeapon = CreateWeapon("locked_weapon");
        WeaponDataSO newWeapon = CreateWeapon("new_weapon");
        TestGameContentProvider provider = new(
            new[] { newWeapon },
            Array.Empty<AccessoryDataSO>(),
            CreateTierWeightProfile(1f, 1f, 1f, 1f));

        GameContentRuntime.SetProvider(provider);
        try
        {
            SetPrivateField(manager, "containersToAdd", 2);
            SetPrivateField(
                manager,
                "currentItems",
                new[]
                {
                    CreateShopItem(lockedWeapon, WeaponLevelHelper.MinLevel, isLocked: true),
                    default
                });

            InvokePrivateMethod(manager, "RefreshKeepingLockedItems");

            ShopItemData[] currentItems = GetPrivateField<ShopItemData[]>(manager, "currentItems");
            Assert.That(currentItems.Length, Is.EqualTo(2));
            Assert.That(currentItems[0].ItemData, Is.SameAs(lockedWeapon));
            Assert.That(currentItems[0].Lock, Is.True);
        }
        finally
        {
            GameContentRuntime.ClearProvider(provider);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void ShopManager_RequestRerollConsumesFreeRerollAndRefillsItems()
    {
        GameObject gameObject = new("ShopManager Test Host");
        ShopManager manager = gameObject.AddComponent<ShopManager>();
        WeaponDataSO weapon = CreateWeapon("reroll_weapon");
        TestGameContentProvider provider = new(
            new[] { weapon },
            Array.Empty<AccessoryDataSO>(),
            CreateTierWeightProfile(1f, 1f, 1f, 1f));

        GameContentRuntime.SetProvider(provider);
        try
        {
            SetPrivateField(manager, "containersToAdd", 1);
            SetPrivateField(manager, "currentCurrency", 0);
            SetPrivateField(manager, "freeShopRerolls", 1);
            SetPrivateField(
                manager,
                "currentItems",
                new[] { CreateShopItem(weapon, WeaponLevelHelper.MinLevel, isLocked: false) });

            manager.RequestReroll();

            ShopItemData[] currentItems = GetPrivateField<ShopItemData[]>(manager, "currentItems");
            Assert.That(GetPrivateField<int>(manager, "freeShopRerolls"), Is.EqualTo(0));
            Assert.That(GetPrivateField<int>(manager, "totalRerollCount"), Is.EqualTo(1));
            Assert.That(currentItems.Length, Is.EqualTo(1));
            Assert.That(currentItems[0].ItemData, Is.SameAs(weapon));
        }
        finally
        {
            GameContentRuntime.ClearProvider(provider);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    private static ShopItemData CreateShopItem(ItemDataSO itemData, int level, bool isLocked)
    {
        return new ShopItemData
        {
            ItemData = itemData,
            Level = level,
            Lock = isLocked,
            SoldOut = false,
            RunPriceMultiplier = 1f,
            PlayerDiscountMultiplier = 1f
        };
    }

    private static ContentTierWeightProfileSO CreateTierWeightProfile(
        float common,
        float rare,
        float epic,
        float legendary)
    {
        ContentTierWeightProfileSO profile = ScriptableObject.CreateInstance<ContentTierWeightProfileSO>();
        SerializedObject serializedObject = new(profile);
        serializedObject.FindProperty("commonWeight").floatValue = common;
        serializedObject.FindProperty("rareWeight").floatValue = rare;
        serializedObject.FindProperty("epicWeight").floatValue = epic;
        serializedObject.FindProperty("legendaryWeight").floatValue = legendary;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return profile;
    }

    private static AccessoryDataSO CreateAccessory(
        string accessoryId,
        ContentTier tier,
        int maxOwnedCount = 0)
    {
        AccessoryDataSO accessory = ScriptableObject.CreateInstance<AccessoryDataSO>();
        accessory.name = accessoryId;
        SerializedObject serializedObject = new(accessory);
        serializedObject.FindProperty("itemName").stringValue = accessoryId;
        serializedObject.FindProperty("itemPrice").intValue = 10;
        serializedObject.FindProperty("itemType").enumValueIndex = (int)ItemType.Accessory;
        serializedObject.FindProperty("accessoryId").stringValue = accessoryId;
        serializedObject.FindProperty("tier").enumValueIndex = (int)tier;
        serializedObject.FindProperty("maxOwnedCount").intValue = maxOwnedCount;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return accessory;
    }

    private static WeaponDataSO CreateWeapon(string weaponId)
    {
        WeaponDataSO weapon = ScriptableObject.CreateInstance<WeaponDataSO>();
        weapon.name = weaponId;
        SerializedObject serializedObject = new(weapon);
        serializedObject.FindProperty("itemName").stringValue = weaponId;
        serializedObject.FindProperty("itemPrice").intValue = 10;
        serializedObject.FindProperty("itemType").enumValueIndex = (int)ItemType.Weapon;
        serializedObject.FindProperty("weaponId").stringValue = weaponId;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return weapon;
    }

    private static AccessoryManager CreateAccessoryManagerWithEquipped(AccessoryDataSO accessory)
    {
        GameObject gameObject = new("AccessoryManager Test Host");
        AccessoryManager manager = gameObject.AddComponent<AccessoryManager>();

        FieldInfo field = typeof(AccessoryManager).GetField(
            "equippedAccessoryDict",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var dictionary = (Dictionary<string, List<Accessory>>)field.GetValue(manager);
        dictionary[accessory.AccessoryId] = new List<Accessory> { new Accessory(accessory) };
        return manager;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
        field.SetValue(target, value);
    }

    private static TValue GetPrivateField<TValue>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
        return (TValue)field.GetValue(target);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Expected private method '{methodName}' to exist.");
        method.Invoke(target, null);
    }

    private static ExtractionCandidate<ShopExtractionCandidate> FindCandidate(
        IReadOnlyList<ExtractionCandidate<ShopExtractionCandidate>> candidates,
        string entryId)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i].EntryId, entryId, StringComparison.Ordinal))
            {
                return candidates[i];
            }
        }

        Assert.Fail($"Expected candidate '{entryId}' was not found.");
        return null;
    }

    private sealed class SequenceExtractionRandom : IExtractionRandom
    {
        private readonly Queue<float> values = new();

        public SequenceExtractionRandom(params float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.values.Enqueue(values[i]);
            }
        }

        public float NextNormalizedValue()
        {
            return values.Count > 0 ? values.Dequeue() : 0f;
        }
    }

    private sealed class TestGameContentProvider : IGameContentProvider
    {
        public TestGameContentProvider(
            IReadOnlyList<WeaponDataSO> weapons,
            IReadOnlyList<AccessoryDataSO> accessories,
            ContentTierWeightProfileSO contentTierWeightProfile)
        {
            Weapons = weapons;
            Accessories = accessories;
            ContentTierWeightProfile = contentTierWeightProfile;
        }

        public IReadOnlyList<WeaponDataSO> Weapons { get; }
        public IReadOnlyList<AccessoryDataSO> Accessories { get; }
        public IReadOnlyList<RewardCardSO> RewardCards => Array.Empty<RewardCardSO>();
        public IReadOnlyList<CollectionSO> Collections => Array.Empty<CollectionSO>();
        public IReadOnlyList<EnemySO> Enemies => Array.Empty<EnemySO>();
        public IReadOnlyList<BuffDataSO> Buffs => Array.Empty<BuffDataSO>();
        public CharacterDataSO DefaultCharacter => null;
        public IReadOnlyList<RewardCardSO> StarterCards => Array.Empty<RewardCardSO>();
        public Player DefaultPlayerPrefab => null;
        public Weapon DefaultWeaponPrefab => null;
        public PlayerLevelConfigSO PlayerLevelConfig => null;
        public RunProgressionProfileSO RunProgressionProfile => null;
        public DropCollectionProfileSO DropCollectionProfile => null;
        public StageDirectorProfileSO DefaultStageDirectorProfile => null;
        public PropPresentationCatalogSO PropPresentationCatalog => null;
        public DamageTextFlow DamageTextPrefab => null;
        public DamageTextVisualConfigSO DamageTextVisualConfig => null;
        public Material ItemQualityIconEffectMaterial => null;
        public TierColorPaletteSO TierColorPalette => null;
        public ContentTierWeightProfileSO ContentTierWeightProfile { get; }
    }
}
