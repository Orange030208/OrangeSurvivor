using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WeaponRuntimeRefactorTests
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
    public void RuntimeStatsResolverMatchesLegacyWeaponFormulaInputs()
    {
        PropertiesManager manager = CreatePropertiesManager(new[]
        {
            new BasePropData(PropType.Damage, 25f),
            new BasePropData(PropType.AttackSpeed, 20f),
            new BasePropData(PropType.CriticalChance, 40f),
            new BasePropData(PropType.CriticalPercent, 60f),
            new BasePropData(PropType.AttackRange, 4f),
            new BasePropData(PropType.KnockbackStrength, 3f),
            new BasePropData(PropType.MeleeAttack, 30f),
            new BasePropData(PropType.RangedAttack, 10f)
        });
        WeaponsHolder holder = CreateGameObject("weapon_holder_bonus").AddComponent<WeaponsHolder>();
        holder.AddWeaponBenefitModifier(
            "bonus",
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 25f,
                criticalChanceBenefitPercent: 0f,
                criticalPercentBenefitPercent: 0f,
                rangeBenefitPercent: 0f,
                knockbackStrengthBenefitPercent: 50f,
                meleeAttackUsagePercent: 5f,
                rangedAttackUsagePercent: 10f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f));
        WeaponDataSO weaponData = CreateWeaponData(
            new WeaponBenefitData(
                attackSpeedBenefitPercent: 50f,
                criticalChanceBenefitPercent: 25f,
                criticalPercentBenefitPercent: 50f,
                rangeBenefitPercent: 100f,
                knockbackStrengthBenefitPercent: 25f,
                meleeAttackUsagePercent: 20f,
                rangedAttackUsagePercent: 0f,
                magicAttackUsagePercent: 0f,
                summonAttackUsagePercent: 0f),
            new WeaponLevelStatData(
                level: 1,
                attack: 10f,
                attackSpeed: 100f,
                criticalChance: 5f,
                criticalPercent: 150f,
                range: 6f,
                knockbackStrength: 2f,
                meleeAttackUsagePercent: 10f,
                rangedAttackUsagePercent: 20f));
        WeaponRuntimeStatsResolver resolver = new();

        WeaponRuntimeStats stats = resolver.Resolve(new WeaponRuntimeStatsRequest(weaponData, 1, manager, holder));

        Assert.That(stats.Damage, Is.EqualTo(29.375f).Within(0.0001f));
        Assert.That(
            stats.AttackInterval,
            Is.EqualTo(PropValueUtility.AttackSpeedPointsToAttackInterval(115f)).Within(0.0001f));
        Assert.That(stats.CriticalChance, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(stats.CriticalMultiplier, Is.EqualTo(1.8f).Within(0.0001f));
        Assert.That(
            stats.Range,
            Is.EqualTo(PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(10f)).Within(0.0001f));
        Assert.That(stats.KnockbackStrength, Is.EqualTo(4.25f).Within(0.0001f));
    }

    [TestCase(ProjectileFiringMode.Default, 1, new[] { 0f })]
    [TestCase(ProjectileFiringMode.Spread, 3, new[] { -30f, 0f, 30f })]
    [TestCase(ProjectileFiringMode.Nova, 4, new[] { 0f, 90f, 180f, 270f })]
    public void ProjectilePatternEmitterEmitsExpectedDirections(
        ProjectileFiringMode firingMode,
        int expectedCount,
        float[] expectedAngles)
    {
        ProjectilePatternEmitter emitter = new();
        List<Vector2> directions = new();
        WeaponSequenceProjectileDefinition config = CreateProjectileConfig(
            firingMode,
            new ProjectilePatternConfig(3, 30f, 3, 4, 0f));

        emitter.Emit(config, CreateProjectileEmissionContext(
            (_, _, _) => CreateProjectile(),
            (IProjectile _, in ProjectileLaunchContext context) => directions.Add(context.Direction)));

        Assert.That(directions, Has.Count.EqualTo(expectedCount));
        for (int i = 0; i < expectedAngles.Length; i++)
        {
            Vector2 expectedDirection = Quaternion.Euler(0f, 0f, expectedAngles[i]) * Vector2.up;
            Assert.That(Vector2.Angle(expectedDirection, directions[i]), Is.LessThan(0.001f));
        }
    }

    [Test]
    public void ProjectilePatternEmitterStartsBurstOncePerActiveBurstId()
    {
        ProjectilePatternEmitter emitter = new();
        int coroutineCount = 0;
        WeaponSequenceProjectileDefinition config = CreateProjectileConfig(
            ProjectileFiringMode.Burst,
            new ProjectilePatternConfig(1, 0f, 3, 1, 0.05f),
            burstId: 7);
        ProjectilePatternEmissionContext context = CreateProjectileEmissionContext(
            (_, _, _) => CreateProjectile(),
            (IProjectile _, in ProjectileLaunchContext _) => { },
            routine =>
            {
                coroutineCount++;
                return null;
            });

        emitter.Emit(config, context);
        emitter.Emit(config, context);

        Assert.That(coroutineCount, Is.EqualTo(1));
    }

    [Test]
    public void ProjectilePatternEmitterBurstRoutineEmitsConfiguredCount()
    {
        ProjectilePatternEmitter emitter = new();
        int launchCount = 0;
        WeaponSequenceProjectileDefinition config = CreateProjectileConfig(
            ProjectileFiringMode.Burst,
            new ProjectilePatternConfig(1, 0f, 3, 1, 0f));
        ProjectilePatternEmissionContext context = CreateProjectileEmissionContext(
            (_, _, _) => CreateProjectile(),
            (IProjectile _, in ProjectileLaunchContext _) => launchCount++);

        IEnumerator routine = emitter.CreateBurstRoutine(config, context);
        while (routine.MoveNext())
        {
        }

        Assert.That(launchCount, Is.EqualTo(3));
    }

    [Test]
    public void HitBoxAttackExecutorSamplesMovementAndDeduplicatesTargets()
    {
        Weapon weapon = CreateGameObject("hitbox_weapon").AddComponent<Weapon>();
        TestEntity source = CreateGameObject("hitbox_source").AddComponent<TestEntity>();
        source.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
        TestEntity target = CreateGameObject("hitbox_target").AddComponent<TestEntity>();
        target.transform.position = new Vector2(1f, 0f);
        HealthComponent healthComponent = target.gameObject.AddComponent<HealthComponent>();
        SetPrivateField(healthComponent, "defaultMaxHealth", 10f);
        healthComponent.Initialize(target);
        target.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
        BoxCollider2D duplicateCollider = target.gameObject.AddComponent<BoxCollider2D>();
        duplicateCollider.size = Vector2.one * 0.2f;
        int layer = LayerMask.NameToLayer("Default");
        target.gameObject.layer = layer;
        Physics2D.SyncTransforms();
        UnityEngine.Random.InitState(1);
        HitBoxAttackExecutor executor = new(null);
        HashSet<HealthComponent> hitTargets = new();
        int sampleCount = 0;

        executor.ExecuteAttack(
            weapon,
            source,
            new HitSpec(1f, 0f, 1f),
            Vector2.one * 0.2f,
            hitTargets,
            1 << layer,
            new HitBoxDetectionPose(Vector2.zero, 0f),
            new HitBoxDetectionPose(new Vector2(1f, 0f), 0f),
            _ => sampleCount++);

        Assert.That(sampleCount, Is.GreaterThan(1));
        Assert.That(hitTargets, Has.Count.EqualTo(1));
        Assert.That(healthComponent.CurrentHealth, Is.EqualTo(9f).Within(0.0001f));
    }

    private ProjectilePatternEmissionContext CreateProjectileEmissionContext(
        ProjectileFactoryHandler createProjectile,
        ProjectileLaunchHandler launchProjectile,
        Func<IEnumerator, Coroutine> startCoroutine = null)
    {
        return new ProjectilePatternEmissionContext(
            new TestProjectileLauncher(),
            () => null,
            () => new HitSpec(1f, 0f, 1f),
            _ => new WeaponSpawnPointPose(Vector3.zero, Quaternion.identity),
            _ => Vector2.up,
            () => 0,
            () => Physics2D.DefaultRaycastLayers,
            () => 5f,
            launchProjectile,
            startCoroutine ?? (_ => null),
            createProjectile);
    }

    private WeaponSequenceProjectileDefinition CreateProjectileConfig(
        ProjectileFiringMode firingMode,
        ProjectilePatternConfig patternConfig,
        int burstId = 0)
    {
        WeaponSequenceProjectileDefinition config = default;
        SetPrivateField(ref config, "projectileDefinition", CreateProjectileDefinition());
        SetPrivateField(ref config, "burstId", burstId);
        SetPrivateField(ref config, "firingMode", firingMode);
        SetPrivateField(ref config, "patternConfig", patternConfig);
        return config;
    }

    private ProjectileDefinitionSO CreateProjectileDefinition()
    {
        ProjectileDefinitionSO projectileDefinition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
        createdObjects.Add(projectileDefinition);
        return projectileDefinition;
    }

    private Projectile CreateProjectile()
    {
        GameObject gameObject = CreateGameObject("projectile");
        gameObject.AddComponent<Rigidbody2D>();
        gameObject.AddComponent<BoxCollider2D>();
        return gameObject.AddComponent<Projectile>();
    }

    private PropertiesManager CreatePropertiesManager(IReadOnlyList<BasePropData> baseProps)
    {
        GameObject gameObject = CreateGameObject("properties_manager_runtime_refactor");
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

    private static void SetPrivateField<TTarget>(ref TTarget target, string fieldName, object value)
        where TTarget : struct
    {
        object boxedTarget = target;
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {typeof(TTarget).Name}.");
        field.SetValue(boxedTarget, value);
        target = (TTarget)boxedTarget;
    }

    private sealed class TestProjectileLauncher : IProjectileLauncher
    {
        public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
        {
            projectile?.Launch(context);
        }
    }

    private sealed class TestEntity : Entity
    {
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
