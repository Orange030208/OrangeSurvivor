using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;

public sealed class GameContentNumericAssetTests
{
    private const float BudgetTolerance = 0.1f;
    private const float MechanicBudgetTolerance = 0.15f;

    [Test]
    public void WeaponTagsUseCurrentCharacteristicSet()
    {
        Assert.AreEqual(0, (int)WeaponTag.Precision);
        Assert.AreEqual(1, (int)WeaponTag.Fast);
        Assert.AreEqual(2, (int)WeaponTag.Heavy);
        Assert.AreEqual(3, (int)WeaponTag.Growth);

        WeaponDataSO[] weapons = LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData);
        Assert.Greater(weapons.Length, 0);

        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponDataSO weapon = weapons[i];
            Assert.Greater(weapon.Tags.Count, 0, weapon.name);

            for (int tagIndex = 0; tagIndex < weapon.Tags.Count; tagIndex++)
            {
                WeaponTag tag = weapon.Tags[tagIndex];
                Assert.IsTrue(Enum.IsDefined(typeof(WeaponTag), tag), $"{weapon.name} has invalid tag value {(int)tag}.");
            }
        }
    }

    [Test]
    public void WeaponAssetsUseLevelTableBenefitsAndDocumentedPriceBands()
    {
        WeaponDataSO[] weapons = LoadAssets<WeaponDataSO>(GameContentAssetPaths.WeaponsData);
        Assert.Greater(weapons.Length, 0);

        for (int i = 0; i < weapons.Length; i++)
        {
            WeaponDataSO weapon = weapons[i];
            Assert.IsTrue(IsWeaponPriceInDocumentBand(weapon.ItemPrice), $"{weapon.name} price {weapon.ItemPrice} is outside weapon price bands.");
            Assert.AreEqual(WeaponLevelHelper.MaxLevel, weapon.LevelStats.Count, weapon.name);
            AssertNonNegativeBenefits(weapon);

            string assetPath = AssetDatabase.GetAssetPath(weapon);
            Assert.IsFalse(File.ReadAllText(assetPath).Contains("attackUsage:"), assetPath);

            for (int level = WeaponLevelHelper.MinLevel; level <= WeaponLevelHelper.MaxLevel; level++)
            {
                WeaponLevelStatData stats = weapon.GetLevelStats(level);
                Assert.AreEqual(level, stats.Level, weapon.name);
                Assert.That(stats.Attack, Is.GreaterThanOrEqualTo(0f), weapon.name);
                Assert.That(stats.AttackSpeed, Is.GreaterThanOrEqualTo(PropValueUtility.MIN_EFFECTIVE_ATTACK_SPEED_POINTS), weapon.name);
                Assert.That(stats.CriticalChance, Is.InRange(0f, 100f), weapon.name);
                Assert.That(stats.CriticalPercent, Is.GreaterThanOrEqualTo(100f), weapon.name);
                Assert.That(stats.Range, Is.GreaterThanOrEqualTo(0f), weapon.name);
                Assert.That(stats.KnockbackStrength, Is.GreaterThanOrEqualTo(0f), weapon.name);
                Assert.That(GetAttackUsageTotal(stats), Is.GreaterThan(0f), $"{weapon.name} level {level} has no attack usage.");
                Assert.NotNull(stats.HolderModifiers, weapon.name);
                AssertHolderModifiersUseAdd(stats, weapon.name);
            }
        }
    }

    [Test]
    public void AccessoryAssetsStayWithinDocumentedBudgetsAndPrices()
    {
        AccessoryDataSO[] accessories = LoadAssets<AccessoryDataSO>(GameContentAssetPaths.AccessoriesData);
        Assert.Greater(accessories.Length, 0);

        for (int i = 0; i < accessories.Length; i++)
        {
            AccessoryDataSO accessory = accessories[i];
            Assert.AreEqual(0, accessory.SpecialFeatures.Count, accessory.name);

            float budget = CalculateBudget(accessory.PropertyModifiers);
            AccessoryBudgetRange range = GetAccessoryBudgetRange(accessory.RarityGrade);

            Assert.That(
                budget,
                Is.InRange(range.Min, range.Max),
                $"{accessory.AccessoryId} {accessory.RarityGrade} budget {budget} is outside expected range {range.Min}-{range.Max}.");

            Assert.AreEqual(GetAccessoryReferencePrice(budget), accessory.ItemPrice, accessory.name);
            AssertAccessoryRecyclePrice(accessory);
        }
    }

    [Test]
    public void EnemyBasePropsMatchFirstWaveNumericSpec()
    {
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Skeleton/SkeletonPropGroup.asset",
            new ExpectedProp(PropType.Attack, 16f),
            new ExpectedProp(PropType.AttackSpeed, 70f),
            new ExpectedProp(PropType.MaxHealth, 35f),
            new ExpectedProp(PropType.DetectionRange, 550f),
            new ExpectedProp(PropType.MoveSpeed, 380f),
            new ExpectedProp(PropType.AttackRange, 160f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Skeleton Meteorhammer/SkeletonMeteorhammerPropGroup.asset",
            new ExpectedProp(PropType.Attack, 20f),
            new ExpectedProp(PropType.AttackSpeed, 60f),
            new ExpectedProp(PropType.MaxHealth, 45f),
            new ExpectedProp(PropType.DetectionRange, 600f),
            new ExpectedProp(PropType.MoveSpeed, 360f),
            new ExpectedProp(PropType.AttackRange, 220f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Worm/WormPropGroup.asset",
            new ExpectedProp(PropType.Attack, 14f),
            new ExpectedProp(PropType.AttackSpeed, 35f),
            new ExpectedProp(PropType.MoveSpeed, 260f),
            new ExpectedProp(PropType.MaxHealth, 35f),
            new ExpectedProp(PropType.DetectionRange, 900f),
            new ExpectedProp(PropType.AttackRange, 750f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/FlyForest/FlyForestPropGroup.asset",
            new ExpectedProp(PropType.Attack, 12f),
            new ExpectedProp(PropType.AttackSpeed, 45f),
            new ExpectedProp(PropType.MaxHealth, 30f),
            new ExpectedProp(PropType.DetectionRange, 900f),
            new ExpectedProp(PropType.MoveSpeed, 300f),
            new ExpectedProp(PropType.AttackRange, 800f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Golem Blue/BlueGolemPropGroup.asset",
            new ExpectedProp(PropType.Attack, 30f),
            new ExpectedProp(PropType.AttackSpeed, 45f),
            new ExpectedProp(PropType.MaxHealth, 100f),
            new ExpectedProp(PropType.DetectionRange, 500f),
            new ExpectedProp(PropType.MoveSpeed, 250f),
            new ExpectedProp(PropType.AttackRange, 210f),
            new ExpectedProp(PropType.Armor, 4f),
            new ExpectedProp(PropType.KnockbackResistance, 40f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Golem Orange/OrangeGolemPropGroup.asset",
            new ExpectedProp(PropType.Attack, 36f),
            new ExpectedProp(PropType.AttackSpeed, 40f),
            new ExpectedProp(PropType.MaxHealth, 115f),
            new ExpectedProp(PropType.DetectionRange, 500f),
            new ExpectedProp(PropType.MoveSpeed, 220f),
            new ExpectedProp(PropType.AttackRange, 220f),
            new ExpectedProp(PropType.Armor, 6f),
            new ExpectedProp(PropType.KnockbackResistance, 50f));
        AssertEnemyProps(
            "Assets/GameContent/Enemies/Data/Golem Mecha Stone/MechaStoneBossPropGroup.asset",
            new ExpectedProp(PropType.Attack, 45f),
            new ExpectedProp(PropType.AttackSpeed, 35f),
            new ExpectedProp(PropType.MaxHealth, 900f),
            new ExpectedProp(PropType.DetectionRange, 700f),
            new ExpectedProp(PropType.MoveSpeed, 180f),
            new ExpectedProp(PropType.AttackRange, 300f),
            new ExpectedProp(PropType.Armor, 10f),
            new ExpectedProp(PropType.KnockbackResistance, 100f));
    }

    [Test]
    public void AuthoredRunProgressionProfileMatchesEnemyScaleSpec()
    {
        RunProgressionProfileSO profile = AssetDatabase.LoadAssetAtPath<RunProgressionProfileSO>(GameContentAssetPaths.RunProgressionProfile);
        Assert.NotNull(profile);

        RunProgressionEnemyScale scale = profile.EvaluateEnemyScale(profile.Evaluate(20, 20, 10f * 60f), null);

        Assert.That(scale.GetMultiplier(PropType.MaxHealth), Is.EqualTo(6f).Within(0.0001f));
        Assert.That(scale.GetMultiplier(PropType.Attack), Is.EqualTo(2.1f).Within(0.0001f));
        Assert.That(scale.GetMultiplier(PropType.MoveSpeed), Is.EqualTo(1.12f).Within(0.0001f));
        Assert.That(scale.GetMultiplier(PropType.AttackSpeed), Is.EqualTo(1.25f).Within(0.0001f));
    }

    private static TAsset[] LoadAssets<TAsset>(string folder)
        where TAsset : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[] { folder });
        List<TAsset> assets = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        return assets.ToArray();
    }

    private static void AssertNonNegativeBenefits(WeaponDataSO weapon)
    {
        WeaponBenefitData benefits = weapon.Benefits;
        Assert.That(benefits.AttackSpeedBenefitPercent, Is.InRange(0f, 100f), weapon.name);
        Assert.That(benefits.CriticalChanceBenefitPercent, Is.InRange(0f, 100f), weapon.name);
        Assert.That(benefits.CriticalPercentBenefitPercent, Is.InRange(0f, 100f), weapon.name);
        Assert.That(benefits.RangeBenefitPercent, Is.InRange(0f, 100f), weapon.name);
        Assert.That(benefits.KnockbackStrengthBenefitPercent, Is.InRange(0f, 100f), weapon.name);
    }

    private static void AssertHolderModifiersUseAdd(WeaponLevelStatData stats, string weaponName)
    {
        for (int i = 0; i < stats.HolderModifiers.Count; i++)
        {
            Assert.AreEqual(PropModifierType.Add, stats.HolderModifiers[i].modifierType, weaponName);
        }
    }

    private static float GetAttackUsageTotal(WeaponLevelStatData stats)
    {
        return stats.MeleeAttackUsagePercent +
               stats.RangedAttackUsagePercent +
               stats.MagicAttackUsagePercent +
               stats.SummonAttackUsagePercent;
    }

    private static bool IsWeaponPriceInDocumentBand(int price)
    {
        return price is >= 20 and <= 30 or >= 33 and <= 45;
    }

    private static float CalculateBudget(IReadOnlyList<PropModifierData> modifiers)
    {
        float budget = 0f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            PropModifierData modifier = modifiers[i];
            Assert.AreEqual(PropModifierType.Add, modifier.modifierType);
            budget += modifier.value * GetPointValue(modifier.propType);
        }

        return budget;
    }

    private static bool ContainsMechanicProp(IReadOnlyList<PropModifierData> modifiers)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            PropType propType = modifiers[i].propType;
            if (propType is PropType.WeaponSlotCount or PropType.ProjectileCount or PropType.ProjectilePierceCount)
            {
                return true;
            }
        }

        return false;
    }

    private static float GetPointValue(PropType propType)
    {
        return propType switch
        {
            PropType.Attack => 25f,
            PropType.Damage => 20f,
            PropType.MeleeAttack => 10f,
            PropType.RangedAttack => 10f,
            PropType.MagicAttack => 10f,
            PropType.SummonAttack => 10f,
            PropType.AttackSpeed => 20f,
            PropType.CriticalChance => 50f,
            PropType.CriticalPercent => 12f,
            PropType.MoveSpeed => 10f,
            PropType.MaxHealth => 8f,
            PropType.HealthRecoverySpeed => 5f,
            PropType.Armor => 80f,
            PropType.Luck => 20f,
            PropType.Dodge => 33f,
            PropType.LifeSteal => 40f,
            PropType.PickupRadius => 8f,
            PropType.ProjectileCount => 300f,
            PropType.ProjectileSpeed => 5f,
            PropType.AttackRange => 10f,
            PropType.ProjectilePierceCount => 200f,
            PropType.KnockbackStrength => 5f,
            PropType.ExperienceGain => 15f,
            PropType.ShopPriceDiscount => 15f,
            PropType.WaveGoldRewardBonus => 12.5f,
            PropType.DamageReduction => 35f,
            PropType.HealingPower => 10f,
            PropType.WeaponSlotCount => 400f,
            _ => throw new AssertionException($"Missing point value for {propType}")
        };
    }

    private static AccessoryBudgetRange GetAccessoryBudgetRange(AccessoryRarity rarity)
    {
        return rarity switch
        {
            AccessoryRarity.Common => new AccessoryBudgetRange(60f, 110f),
            AccessoryRarity.Rare => new AccessoryBudgetRange(110f, 180f),
            AccessoryRarity.Epic => new AccessoryBudgetRange(180f, 280f),
            AccessoryRarity.Legendary => new AccessoryBudgetRange(280f, 500f),
            _ => new AccessoryBudgetRange(0f, 0f)
        };
    }

    private static int GetAccessoryReferencePrice(float budget)
    {
        return (int)Math.Round(Math.Max(0f, budget) / 4f, MidpointRounding.AwayFromZero);
    }

    private static void AssertAccessoryRecyclePrice(AccessoryDataSO accessory)
    {
        int price = Math.Max(0, accessory.ItemPrice);
        int min = (int)Math.Floor(price * 0.25f);
        int max = (int)Math.Ceiling(price * 0.35f);
        Assert.That(
            accessory.RecyclePrice,
            Is.InRange(min, max),
            $"{accessory.name} recycle price should stay near 25%-35% of item price {price}.");
    }

    private static void AssertEnemyProps(string assetPath, params ExpectedProp[] expectedProps)
    {
        BasePropGroupSO propGroup = AssetDatabase.LoadAssetAtPath<BasePropGroupSO>(assetPath);
        Assert.NotNull(propGroup, assetPath);
        Dictionary<PropType, float> values = ToDictionary(propGroup.Values);

        for (int i = 0; i < expectedProps.Length; i++)
        {
            ExpectedProp expected = expectedProps[i];
            Assert.IsTrue(values.TryGetValue(expected.PropType, out float actual), $"{assetPath} missing {expected.PropType}.");
            Assert.That(actual, Is.EqualTo(expected.Value).Within(0.0001f), $"{assetPath} {expected.PropType}");
        }
    }

    private static Dictionary<PropType, float> ToDictionary(IReadOnlyList<BasePropData> props)
    {
        Dictionary<PropType, float> values = new();
        for (int i = 0; i < props.Count; i++)
        {
            values[props[i].propType] = props[i].value;
        }

        return values;
    }

    private readonly struct ExpectedProp
    {
        public ExpectedProp(PropType propType, float value)
        {
            PropType = propType;
            Value = value;
        }

        public PropType PropType { get; }
        public float Value { get; }
    }

    private readonly struct AccessoryBudgetRange
    {
        public AccessoryBudgetRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }
    }
}
