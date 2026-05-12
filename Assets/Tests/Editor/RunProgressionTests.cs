using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RunProgressionTests
{
    private readonly List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void DefaultProgressionCurvesAreMonotonicAndFinite()
    {
        RunProgressionProfileSO profile = CreateProfile();
        RunProgressionSnapshot previous = profile.Evaluate(1, 20, 0f);
        AssertSnapshotFinite(previous);

        for (int wave = 2; wave <= 20; wave++)
        {
            RunProgressionSnapshot snapshot = profile.Evaluate(wave, 20, wave * 30f);
            AssertSnapshotFinite(snapshot);
            Assert.That(snapshot.DifficultyCoefficient, Is.GreaterThanOrEqualTo(previous.DifficultyCoefficient - 0.0001f));
            Assert.That(snapshot.EconomyCoefficient, Is.GreaterThanOrEqualTo(previous.EconomyCoefficient - 0.0001f));
            Assert.That(snapshot.ShopPriceMultiplier, Is.GreaterThanOrEqualTo(previous.ShopPriceMultiplier - 0.0001f));
            previous = snapshot;
        }

        RunProgressionSnapshot wave25 = profile.Evaluate(25, 20, 25f * 30f);
        RunProgressionSnapshot wave40 = profile.Evaluate(40, 20, 40f * 30f);
        AssertSnapshotFinite(wave25);
        AssertSnapshotFinite(wave40);
        Assert.IsTrue(wave25.IsEndlessWave);
        Assert.That(wave40.DifficultyCoefficient, Is.GreaterThan(wave25.DifficultyCoefficient));
        Assert.That(wave40.EconomyCoefficient, Is.GreaterThan(wave25.EconomyCoefficient));
        Assert.That(wave40.ShopPriceMultiplier, Is.GreaterThan(wave25.ShopPriceMultiplier));
    }

    [Test]
    public void CoinRewardDataKeepsFixedGoldAndExperienceValues()
    {
        CoinRewardData reward = new(1, 1);

        Assert.AreEqual(1, reward.GoldValue);
        Assert.AreEqual(1, reward.ExperienceValue);
        Assert.IsTrue(reward.HasAnyReward);
    }

    [Test]
    public void ShopPricingStacksLevelContentRunAndDiscountMultipliers()
    {
        AccessoryDataSO accessory = CreateItem<AccessoryDataSO>(100, ItemType.Accessory);

        int clampedDiscountPrice = ShopPricingService.GetPrice(accessory, 1, 1.5f, 2f, 0.2f);
        int regularDiscountPrice = ShopPricingService.GetPrice(accessory, 1, 1.5f, 2f, 0.75f);

        Assert.AreEqual(150, clampedDiscountPrice);
        Assert.AreEqual(225, regularDiscountPrice);
    }

    [Test]
    public void WeaponPriceUsesConfiguredLevelMultiplierTable()
    {
        Assert.AreEqual(20, WeaponPriceHelper.GetPrice(20, 1));
        Assert.AreEqual(36, WeaponPriceHelper.GetPrice(20, 2));
        Assert.AreEqual(76, WeaponPriceHelper.GetPrice(20, 3));
        Assert.AreEqual(156, WeaponPriceHelper.GetPrice(20, 4));
        Assert.AreEqual(195, WeaponPriceHelper.GetPrice(25, 4));
        Assert.That(WeaponPriceHelper.GetLevelPriceMultiplier(4), Is.EqualTo(7.8f).Within(0.0001f));
    }

    [Test]
    public void ShopPricingUsesWeaponLevelPriceBeforeShopMultipliers()
    {
        WeaponDataSO weapon = CreateItem<WeaponDataSO>(20, ItemType.Weapon);

        int price = ShopPricingService.GetPrice(weapon, 3, 1.5f, 2f, 1f);

        Assert.AreEqual(228, price);
    }

    [Test]
    public void EnemyScaleIncreasesWithDifficultyWithoutMutatingEnemyAsset()
    {
        RunProgressionProfileSO profile = CreateProfile();
        WormEnemySO enemyData = ScriptableObject.CreateInstance<WormEnemySO>();
        createdObjects.Add(enemyData);
        enemyData.role = EnemyRole.Normal;

        RunProgressionEnemyScale early = profile.EvaluateEnemyScale(profile.Evaluate(1, 20, 0f), enemyData);
        RunProgressionEnemyScale late = profile.EvaluateEnemyScale(profile.Evaluate(20, 20, 10f * 60f), enemyData);

        Assert.That(late.GetMultiplier(PropType.MaxHealth), Is.GreaterThan(early.GetMultiplier(PropType.MaxHealth)));
        Assert.That(late.GetMultiplier(PropType.Attack), Is.GreaterThan(early.GetMultiplier(PropType.Attack)));
        Assert.That(late.GetMultiplier(PropType.MoveSpeed), Is.GreaterThanOrEqualTo(early.GetMultiplier(PropType.MoveSpeed)));
        Assert.That(late.GetMultiplier(PropType.AttackSpeed), Is.GreaterThanOrEqualTo(early.GetMultiplier(PropType.AttackSpeed)));
        Assert.AreEqual(EnemyRole.Normal, enemyData.role);
    }

    [Test]
    public void EnemyScaleSupportsCustomPropCurve()
    {
        RunProgressionProfileSO profile = CreateProfile();
        SetPrivateField(
            profile,
            "enemyPropScaleCurves",
            new List<RunProgressionPropScaleCurve>
            {
                new(PropType.Armor, new AnimationCurve(
                    new Keyframe(1f, 1f),
                    new Keyframe(3.6f, 2.5f)))
            });

        RunProgressionEnemyScale early = profile.EvaluateEnemyScale(profile.Evaluate(1, 20, 0f), null);
        RunProgressionEnemyScale late = profile.EvaluateEnemyScale(profile.Evaluate(20, 20, 10f * 60f), null);
        List<PropModifierData> modifiers = RunProgressionEnemyScaling.BuildModifiers(late);

        Assert.That(late.GetMultiplier(PropType.Armor), Is.GreaterThan(early.GetMultiplier(PropType.Armor)));
        Assert.That(modifiers.Exists(modifier =>
            modifier.propType == PropType.Armor &&
            modifier.modifierType == PropModifierType.FinalMultiplier &&
            modifier.value > 0f));
    }

    [Test]
    public void BossAndTagsApplyConfigurablePropMultipliers()
    {
        RunProgressionProfileSO profile = CreateProfile();
        WormEnemySO normalData = ScriptableObject.CreateInstance<WormEnemySO>();
        WormEnemySO bossData = ScriptableObject.CreateInstance<WormEnemySO>();
        createdObjects.Add(normalData);
        createdObjects.Add(bossData);
        normalData.role = EnemyRole.Normal;
        bossData.role = EnemyRole.Boss;

        RunProgressionSnapshot snapshot = profile.Evaluate(20, 20, 10f * 60f);
        RunProgressionEnemyScale normal = profile.EvaluateEnemyScale(snapshot, normalData);
        RunProgressionEnemyScale boss = profile.EvaluateEnemyScale(snapshot, bossData);
        RunProgressionEnemyScale tagged = profile.EvaluateEnemyScale(
            snapshot,
            normalData,
            WaveEnemyTag.Elite | WaveEnemyTag.Fast);

        Assert.That(
            boss.GetMultiplier(PropType.MaxHealth),
            Is.EqualTo(normal.GetMultiplier(PropType.MaxHealth) * 1.35f).Within(0.0001f));
        Assert.That(
            boss.GetMultiplier(PropType.Attack),
            Is.EqualTo(normal.GetMultiplier(PropType.Attack) * 1.15f).Within(0.0001f));
        Assert.That(
            tagged.GetMultiplier(PropType.MaxHealth),
            Is.EqualTo(normal.GetMultiplier(PropType.MaxHealth) * 1.18f).Within(0.0001f));
        Assert.That(
            tagged.GetMultiplier(PropType.Attack),
            Is.EqualTo(normal.GetMultiplier(PropType.Attack) * 1.12f).Within(0.0001f));
        Assert.That(
            tagged.GetMultiplier(PropType.MoveSpeed),
            Is.EqualTo(normal.GetMultiplier(PropType.MoveSpeed) * 1.08f).Within(0.0001f));
    }

    [Test]
    public void ContentFactsExposeProgressionSnapshot()
    {
        RunProgressionSnapshot snapshot = new(
            25,
            20,
            12f,
            1,
            4f,
            2.5f,
            3f,
            5);
        ContentFactSet facts = ContentFactCollector.Collect(
            new ContentFactSource
            {
                WaveNumber = snapshot.WaveNumber,
                ProgressionSnapshot = snapshot
            },
            null);

        AssertFactFloat(facts, ContentFactIds.DifficultyCoefficient, 4f);
        AssertFactFloat(facts, ContentFactIds.EconomyCoefficient, 2.5f);
        AssertFactFloat(facts, ContentFactIds.ShopPriceMultiplier, 3f);
        AssertFactInt(facts, ContentFactIds.EndlessLoop, 1);
        AssertFactBool(facts, ContentFactIds.EndlessWave, true);
        AssertFactInt(facts, ContentFactIds.DangerTier, 5);
    }

    private RunProgressionProfileSO CreateProfile()
    {
        RunProgressionProfileSO profile = RunProgressionProfileSO.CreateRuntimeDefault();
        createdObjects.Add(profile);
        return profile;
    }

    private TItem CreateItem<TItem>(int price, ItemType itemType)
        where TItem : ItemDataSO
    {
        TItem item = ScriptableObject.CreateInstance<TItem>();
        createdObjects.Add(item);
        SetPrivateField(item, "itemPrice", price);
        SetPrivateField(item, "itemType", itemType);
        return item;
    }

    private static void AssertSnapshotFinite(RunProgressionSnapshot snapshot)
    {
        AssertFiniteNonNegative(snapshot.DifficultyCoefficient);
        AssertFiniteNonNegative(snapshot.EconomyCoefficient);
        AssertFiniteNonNegative(snapshot.ShopPriceMultiplier);
        Assert.That(snapshot.DangerTier, Is.GreaterThanOrEqualTo(0));
    }

    private static void AssertFiniteNonNegative(float value)
    {
        Assert.IsFalse(float.IsNaN(value));
        Assert.IsFalse(float.IsInfinity(value));
        Assert.That(value, Is.GreaterThanOrEqualTo(0f));
    }

    private static void AssertFactFloat(ContentFactSet facts, string factId, float expected)
    {
        Assert.IsTrue(facts.TryGet(factId, out ContentFactValue value), $"Missing fact '{factId}'.");
        Assert.That(value.FloatValue, Is.EqualTo(expected).Within(0.0001f));
    }

    private static void AssertFactInt(ContentFactSet facts, string factId, int expected)
    {
        Assert.IsTrue(facts.TryGet(factId, out ContentFactValue value), $"Missing fact '{factId}'.");
        Assert.AreEqual(expected, value.IntValue);
    }

    private static void AssertFactBool(ContentFactSet facts, string factId, bool expected)
    {
        Assert.IsTrue(facts.TryGet(factId, out ContentFactValue value), $"Missing fact '{factId}'.");
        Assert.AreEqual(expected, value.BoolValue);
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (System.Type type = typeof(TTarget); type != null && field == null; type = type.BaseType)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }
}
