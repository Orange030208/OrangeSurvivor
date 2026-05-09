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
    public void WeaponAttackUsageDataAddsByPercentPoints()
    {
        WeaponAttackUsageData baseUsage = new(20f, 0f, 5f, 0f);
        WeaponAttackUsageData bonusUsage = new(20f, 10f, 0f, 3f);

        WeaponAttackUsageData result = baseUsage + bonusUsage;

        Assert.That(result.MeleeAttackUsagePercent, Is.EqualTo(40f).Within(0.0001f));
        Assert.That(result.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));
        Assert.That(result.MagicAttackUsagePercent, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(result.SummonAttackUsagePercent, Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    public void WeaponsHolderAggregatesAndRemovesAttackUsageModifiersBySource()
    {
        GameObject gameObject = CreateGameObject("weapon_usage_holder");
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();

        holder.AddWeaponAttackUsageModifier("source_a", new WeaponAttackUsageData(20f, 0f, 0f, 0f));
        holder.AddWeaponAttackUsageModifier("source_b", new WeaponAttackUsageData(5f, 10f, 0f, 0f));

        Assert.That(holder.CurrentAttackUsageBonus.MeleeAttackUsagePercent, Is.EqualTo(25f).Within(0.0001f));
        Assert.That(holder.CurrentAttackUsageBonus.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));

        holder.RemoveWeaponAttackUsageModifier("source_a");

        Assert.That(holder.CurrentAttackUsageBonus.MeleeAttackUsagePercent, Is.EqualTo(5f).Within(0.0001f));
        Assert.That(holder.CurrentAttackUsageBonus.RangedAttackUsagePercent, Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void WeaponAttackUsageModifierFeatureInstallsAndUninstallsHolderBonus()
    {
        GameObject gameObject = CreateGameObject("weapon_usage_feature");
        TestPropertyEntity entity = gameObject.AddComponent<TestPropertyEntity>();
        WeaponsHolder holder = gameObject.AddComponent<WeaponsHolder>();
        WeaponAttackUsageModifierFeature feature = new(new WeaponAttackUsageData(20f, 0f, 0f, 0f))
        {
            Context = new FeatureContext(entity, null),
            SourceId = "feature_source"
        };

        feature.OnInstall();

        Assert.That(holder.CurrentAttackUsageBonus.MeleeAttackUsagePercent, Is.EqualTo(20f).Within(0.0001f));

        feature.OnUninstall();

        Assert.That(holder.CurrentAttackUsageBonus.MeleeAttackUsagePercent, Is.EqualTo(0f).Within(0.0001f));
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
