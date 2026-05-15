using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class PropertyMappingAndAttackUsageTests
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
    public void AttackMappingAddsAllTypedAttackProps()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.Attack, 10f)
        });

        Assert.That(manager.GetPropValue(PropType.MeleeAttack), Is.EqualTo(10f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.RangedAttack), Is.EqualTo(10f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.MagicAttack), Is.EqualTo(10f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.SummonAttack), Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void MappingSourceUsesUnmappedFinalValue()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.Attack, 10f)
        });

        manager.AddModifier(
            "attack_bonus",
            new PropModifierData(PropType.Attack, PropModifierType.BonusMultiplier, 50f));

        Assert.That(manager.GetPropValue(PropType.Attack), Is.EqualTo(15f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.MeleeAttack), Is.EqualTo(15f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.RangedAttack), Is.EqualTo(15f).Within(0.0001f));
    }

    [Test]
    public void MappingContributionParticipatesInTargetMultipliers()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.Attack, 10f)
        });

        manager.AddModifier(
            "melee_bonus",
            new PropModifierData(PropType.MeleeAttack, PropModifierType.BonusMultiplier, 20f));

        Assert.That(manager.GetPropValue(PropType.MeleeAttack), Is.EqualTo(12f).Within(0.0001f));
        Assert.That(manager.GetPropValue(PropType.RangedAttack), Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void ChangingMappingSourceNotifiesMappedTargets()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.Attack, 10f)
        });
        HashSet<PropType> changedProps = new();
        manager.OnPropertyChanged += (propType, _) => changedProps.Add(propType);

        manager.AddModifier(
            "attack_add",
            new PropModifierData(PropType.Attack, PropModifierType.Add, 5f));

        Assert.IsTrue(changedProps.Contains(PropType.Attack));
        Assert.IsTrue(changedProps.Contains(PropType.MeleeAttack));
        Assert.IsTrue(changedProps.Contains(PropType.RangedAttack));
        Assert.IsTrue(changedProps.Contains(PropType.MagicAttack));
        Assert.IsTrue(changedProps.Contains(PropType.SummonAttack));
    }

    [Test]
    public void AttackSpeedAddUsesFlatPoints()
    {
        PropertiesManager manager = CreatePropertiesManager(System.Array.Empty<BasePropData>());

        Assert.That(manager.GetPropValue(PropType.AttackSpeed), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(manager.GetPropValueWithAdditionalBase(PropType.AttackSpeed, 10f), Is.EqualTo(10f).Within(0.0001f));

        manager.AddModifier(
            "attack_speed_bonus",
            new PropModifierData(PropType.AttackSpeed, PropModifierType.Add, 20f));

        Assert.That(manager.GetPropValueWithAdditionalBase(PropType.AttackSpeed, 10f), Is.EqualTo(30f).Within(0.0001f));
    }

    [Test]
    public void AttackSpeedPointsConvertToAttackInterval()
    {
        Assert.That(PropValueUtility.AttackSpeedPointsToAttacksPerSecond(100f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttacksPerSecond(0f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttackInterval(100f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttackInterval(120f), Is.EqualTo(0.8333333f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttackInterval(50f), Is.EqualTo(2f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttackInterval(0f), Is.EqualTo(100f).Within(0.0001f));
        Assert.That(PropValueUtility.AttackSpeedPointsToAttackInterval(-20f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void PropValueUtilityOwnsRuntimeValueBoundaries()
    {
        Assert.That(PropValueUtility.ClampEffectiveMaxHealth(-10f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PropValueUtility.PercentPointsToNonNegativeRatio(-25f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(PropValueUtility.HealthRecoveryPointsToEffectiveHealthPerSecond(-10f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(PropValueUtility.DistancePointsToNonNegativeWorldUnits(-10f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(-10f), Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(PropValueUtility.ClampEffectiveCriticalMultiplier(0.5f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PropValueUtility.ClampNonNegative(-1f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(PropValueUtility.ResolveArmorDamageReductionRatio(25f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(PropValueUtility.CombineDamageReductionRatios(0.8f, 0.8f), Is.EqualTo(0.95f).Within(0.0001f));
        Assert.That(PropValueUtility.FloatPointsToNonNegativeRoundedInt(2.6f), Is.EqualTo(3));
        Assert.That(PropValueUtility.FloatPointsToNonNegativeFlooredInt(2.9f), Is.EqualTo(2));
        Assert.That(PropValueUtility.ResolveEffectiveShopPriceMultiplier(0.2f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(PropValueUtility.ResolveNonNegativePrice(-20f), Is.EqualTo(0));
    }

    [Test]
    public void WeaponBenefitDataAddsAttackBenefitsByPercentPoints()
    {
        WeaponBenefitData baseUsage = new(
            attackSpeedBenefitPercent: 100f,
            criticalChanceBenefitPercent: 100f,
            criticalPercentBenefitPercent: 100f,
            rangeBenefitPercent: 100f,
            knockbackStrengthBenefitPercent: 100f,
            meleeAttackUsagePercent: 20f,
            rangedAttackUsagePercent: 0f,
            magicAttackUsagePercent: 5f,
            summonAttackUsagePercent: 0f);
        WeaponBenefitData bonusUsage = new(
            attackSpeedBenefitPercent: 0f,
            criticalChanceBenefitPercent: 0f,
            criticalPercentBenefitPercent: 0f,
            rangeBenefitPercent: 0f,
            knockbackStrengthBenefitPercent: 0f,
            meleeAttackUsagePercent: 20f,
            rangedAttackUsagePercent: 10f,
            magicAttackUsagePercent: 0f,
            summonAttackUsagePercent: 3f);

        WeaponBenefitData result = baseUsage + bonusUsage;

        Assert.That(result.MeleeAttackUsagePercent, Is.EqualTo(40f).Within(0.0001f));
        Assert.That(result.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(result.MagicAttackUsagePercent, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(result.SummonAttackUsagePercent, Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void WeaponStatBenefitScalesOnlyExternalResolvedContribution()
    {
        WeaponBenefitData benefitData = new(
            attackSpeedBenefitPercent: 50f,
            criticalChanceBenefitPercent: 100f,
            criticalPercentBenefitPercent: 100f,
            rangeBenefitPercent: 100f,
            knockbackStrengthBenefitPercent: 25f,
            meleeAttackUsagePercent: 0f,
            rangedAttackUsagePercent: 0f,
            magicAttackUsagePercent: 0f,
            summonAttackUsagePercent: 0f);

        Assert.That(
            benefitData.ApplyToResolvedStat(PropType.AttackSpeed, 300f, 360f),
            Is.EqualTo(330f).Within(0.0001f));
        Assert.That(
            benefitData.ApplyToResolvedStat(PropType.KnockbackStrength, 2f, 6f),
            Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void WeaponStatBenefitScalesCriticalExternalValue()
    {
        WeaponBenefitData benefitData = new(
            attackSpeedBenefitPercent: 100f,
            criticalChanceBenefitPercent: 25f,
            criticalPercentBenefitPercent: 50f,
            rangeBenefitPercent: 100f,
            knockbackStrengthBenefitPercent: 100f,
            meleeAttackUsagePercent: 0f,
            rangedAttackUsagePercent: 0f,
            magicAttackUsagePercent: 0f,
            summonAttackUsagePercent: 0f);

        Assert.That(
            benefitData.ApplyToExternalValue(PropType.CriticalChance, 0.4f),
            Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(
            benefitData.ApplyToExternalValue(PropType.CriticalPercent, 0.8f),
            Is.EqualTo(0.4f).Within(0.0001f));
    }

    [Test]
    public void WeaponsHolderAggregatesAndRemovesWeaponBenefitModifiersBySource()
    {
        GameObject gameObject = CreateGameObject("weapon_usage_holder");
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();

        holder.AddWeaponBenefitModifier(
            "source_a",
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 25f,
                criticalChanceBenefitPercent: 0f,
                criticalPercentBenefitPercent: 0f,
                rangeBenefitPercent: 0f,
                knockbackStrengthBenefitPercent: 0f,
                meleeAttackUsagePercent: 20f,
                rangedAttackUsagePercent: 0f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f));
        holder.AddWeaponBenefitModifier(
            "source_b",
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 5f,
                criticalChanceBenefitPercent: 10f,
                criticalPercentBenefitPercent: 0f,
                rangeBenefitPercent: 0f,
                knockbackStrengthBenefitPercent: 0f,
                meleeAttackUsagePercent: 5f,
                rangedAttackUsagePercent: 10f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f));

        Assert.That(holder.CurrentWeaponBenefitBonus.AttackSpeedBenefitPercent, Is.EqualTo(30f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.CriticalChanceBenefitPercent, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.MeleeAttackUsagePercent, Is.EqualTo(25f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));

        holder.RemoveWeaponBenefitModifier("source_a");

        Assert.That(holder.CurrentWeaponBenefitBonus.AttackSpeedBenefitPercent, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.CriticalChanceBenefitPercent, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.MeleeAttackUsagePercent, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void WeaponsHolderRaisesBenefitChangedEventForStatOnlyModifier()
    {
        GameObject gameObject = CreateGameObject("weapon_usage_holder_event");
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();
        int eventCount = 0;

        holder.OnWeaponBenefitBonusChanged += () => eventCount++;
        holder.AddWeaponBenefitModifier(
            "source_a",
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 50f,
                criticalChanceBenefitPercent: 0f,
                criticalPercentBenefitPercent: 0f,
                rangeBenefitPercent: 0f,
                knockbackStrengthBenefitPercent: 0f,
                meleeAttackUsagePercent: 0f,
                rangedAttackUsagePercent: 0f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f));

        holder.RemoveWeaponBenefitModifier("source_a");

        Assert.That(eventCount, Is.EqualTo(2));
    }

    [Test]
    public void WeaponUsesHolderBenefitModifiersForRuntimeStatResolution()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.AttackSpeed, 20f)
        });

        GameObject gameObject = CreateGameObject("weapon_benefit_runtime");
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();
        Weapon weapon = gameObject.AddComponent<Weapon>();
        WeaponDataSO weaponData = CreateWeaponData(
            WeaponBenefitData.Full,
            new WeaponLevelStatData(
                level: 1,
                attack: 10f,
                attackSpeed: 100f,
                criticalChance: 0f,
                criticalPercent: 100f,
                range: 0f,
                knockbackStrength: 0f));

        SetPrivateField(weapon, "propertiesManager", manager);
        SetPrivateField(weapon, "weaponsHolder", holder);
        SetPrivateField(weapon, "<WeaponData>k__BackingField", weaponData);

        holder.AddWeaponBenefitModifier(
            "source_a",
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 50f,
                criticalChanceBenefitPercent: 0f,
                criticalPercentBenefitPercent: 0f,
                rangeBenefitPercent: 0f,
                knockbackStrengthBenefitPercent: 0f,
                meleeAttackUsagePercent: 0f,
                rangedAttackUsagePercent: 0f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f));

        weapon.RefreshRuntimeStats();

        Assert.That(
            weapon.AttackInterval,
            Is.EqualTo(PropValueUtility.AttackSpeedPointsToAttackInterval(130f)).Within(0.0001f));
    }

    [Test]
    public void WeaponLevelStatsProvideAttackUsageForRuntimeDamage()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.MeleeAttack, 20f),
            new BasePropData(PropType.RangedAttack, 10f)
        });

        GameObject gameObject = CreateGameObject("weapon_level_attack_usage_runtime");
        Weapon weapon = gameObject.AddComponent<Weapon>();
        WeaponDataSO weaponData = CreateWeaponData(
            WeaponBenefitData.Full,
            new WeaponLevelStatData(
                level: 1,
                attack: 10f,
                attackSpeed: 100f,
                criticalChance: 0f,
                criticalPercent: 100f,
                range: 0f,
                knockbackStrength: 0f,
                meleeAttackUsagePercent: 50f,
                rangedAttackUsagePercent: 20f));

        SetPrivateField(weapon, "propertiesManager", manager);
        SetPrivateField(weapon, "<WeaponData>k__BackingField", weaponData);

        weapon.RefreshRuntimeStats();

        Assert.That(weapon.Damage, Is.EqualTo(22f).Within(0.0001f));
    }

    [Test]
    public void WeaponLevelStatsApplyHolderModifiersByLevel()
    {
        PropertiesManager manager = CreatePropertiesManager(System.Array.Empty<BasePropData>());

        GameObject gameObject = CreateGameObject("weapon_level_holder_modifiers_runtime");
        Weapon weapon = gameObject.AddComponent<Weapon>();
        SetPrivateField(weapon, "propertiesManager", manager);
        SetPrivateField(weapon, "<WeaponData>k__BackingField", CreateWeaponData(
            WeaponBenefitData.Full,
            new WeaponLevelStatData(
                level: 1,
                attack: 10f,
                attackSpeed: 100f,
                criticalChance: 0f,
                criticalPercent: 100f,
                range: 0f,
                knockbackStrength: 0f,
                holderModifiers: new[]
                {
                    new PropModifierData(PropType.Damage, 15f)
                })));

        weapon.SetLevel(1);

        Assert.That(manager.GetPropValue(PropType.Damage), Is.EqualTo(15f).Within(0.0001f));

        weapon.OnDisableComponent();

        Assert.That(manager.GetPropValue(PropType.Damage), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void WeaponStopForWaveCleanupKeepsHolderModifiers()
    {
        PropertiesManager manager = CreatePropertiesManager(System.Array.Empty<BasePropData>());

        GameObject gameObject = CreateGameObject("weapon_cleanup_holder_modifiers_runtime");
        Weapon weapon = gameObject.AddComponent<Weapon>();
        SetPrivateField(weapon, "propertiesManager", manager);
        SetPrivateField(weapon, "<WeaponData>k__BackingField", CreateWeaponData(
            WeaponBenefitData.Full,
            new WeaponLevelStatData(
                level: 1,
                attack: 10f,
                attackSpeed: 100f,
                criticalChance: 0f,
                criticalPercent: 100f,
                range: 0f,
                knockbackStrength: 0f,
                holderModifiers: new[]
                {
                    new PropModifierData(PropType.Damage, 15f)
                })));

        weapon.SetLevel(1);

        weapon.StopForWaveCleanup();

        Assert.That(manager.GetPropValue(PropType.Damage), Is.EqualTo(15f).Within(0.0001f));

        weapon.OnDisableComponent();

        Assert.That(manager.GetPropValue(PropType.Damage), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void WeaponBenefitBonusModifierFeatureInstallsAndUninstallsHolderBonus()
    {
        GameObject gameObject = CreateGameObject("weapon_usage_feature");
        TestPropertyEntity entity = gameObject.AddComponent<TestPropertyEntity>();
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();
        WeaponBenefitBonusModifierFeature feature = new(new WeaponBenefitData(
            attackSpeedBenefitPercent: 50f,
            criticalChanceBenefitPercent: 0f,
            criticalPercentBenefitPercent: 0f,
            rangeBenefitPercent: 0f,
            knockbackStrengthBenefitPercent: 0f,
            meleeAttackUsagePercent: 20f,
            rangedAttackUsagePercent: 0f,
            magicAttackUsagePercent: 0f,
            summonAttackUsagePercent: 0f))
        {
            Context = new FeatureContext(entity, null),
            SourceId = "feature_source"
        };

        feature.OnInstall();

        Assert.That(holder.CurrentWeaponBenefitBonus.AttackSpeedBenefitPercent, Is.EqualTo(50f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.MeleeAttackUsagePercent, Is.EqualTo(20f).Within(0.0001f));

        feature.OnUninstall();

        Assert.That(holder.CurrentWeaponBenefitBonus.AttackSpeedBenefitPercent, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(holder.CurrentWeaponBenefitBonus.MeleeAttackUsagePercent, Is.EqualTo(0f).Within(0.0001f));
    }

    private PropertiesManager CreatePropertiesManager(IReadOnlyList<BasePropData> baseProps)
    {
        GameObject gameObject = CreateGameObject("properties_manager_mapping");
        TestPropertyEntity entity = gameObject.AddComponent<TestPropertyEntity>();
        entity.Configure(CreateBasePropGroup(baseProps), new List<PropModifierData>());
        PropertiesManager manager = gameObject.AddComponent<PropertiesManager>();
        SetPrivateField(manager, "propMappings", CreateDefaultMappings());

        manager.Initialize(entity);
        return manager;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private BasePropGroupSO CreateBasePropGroup(IReadOnlyList<BasePropData> values)
    {
        BasePropGroupSO group = ScriptableObject.CreateInstance<BasePropGroupSO>();
        createdObjects.Add(group);
        SetPrivateField(group, "values", new List<BasePropData>(values));
        return group;
    }

    private WeaponDataSO CreateWeaponData(WeaponBenefitData benefits, WeaponLevelStatData levelStats)
    {
        WeaponDataSO weaponData = ScriptableObject.CreateInstance<WeaponDataSO>();
        createdObjects.Add(weaponData);
        SetPrivateField(weaponData, "benefits", benefits);
        SetPrivateField(weaponData, "levelStats", new List<WeaponLevelStatData> { levelStats });
        return weaponData;
    }

    private static List<PropMappingData> CreateDefaultMappings()
    {
        return new List<PropMappingData>
        {
            new(PropType.Attack, PropType.MeleeAttack, 100f),
            new(PropType.Attack, PropType.RangedAttack, 100f),
            new(PropType.Attack, PropType.MagicAttack, 100f),
            new(PropType.Attack, PropType.SummonAttack, 100f)
        };
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }

    private sealed class TestPropertyEntity : Entity, IPropGroupProvider, IPropModifierProvider
    {
        private BasePropGroupSO basePropsGroup;
        private IReadOnlyList<PropModifierData> propModifierDataList = new List<PropModifierData>();

        public BasePropGroupSO BasePropsGroup => basePropsGroup;
        public IReadOnlyList<PropModifierData> PropModifierDataList => propModifierDataList;

        public void Configure(BasePropGroupSO basePropsGroup, IReadOnlyList<PropModifierData> propModifierDataList)
        {
            this.basePropsGroup = basePropsGroup;
            this.propModifierDataList = propModifierDataList ?? new List<PropModifierData>();
        }
    }
}
