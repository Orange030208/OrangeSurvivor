using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WeaponTypeRangeDwellMaxHealthFeatureTests
{
    private const int TargetLayer = 0;
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
    public void DamageAfterRequiredDwellAddsMaxHealthAndResetsTarget()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(10f).Within(0.0001f));

        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void FutureMatchingWeaponIsSubscribedThroughWeaponsChanged()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 7f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(7f).Within(0.0001f));
    }

    [Test]
    public void NonMatchingWeaponDoesNotTrigger()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("axe", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void LingerAllowsTriggerBrieflyAfterLeavingRange()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.5f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(0.5f);
        target.transform.position = new Vector2(5f, 0f);
        Physics2D.SyncTransforms();
        feature.OnUpdate(0.25f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void LingerExpiryClearsPrimedTarget()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.5f,
            lingerSeconds: 0.25f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(0.5f);
        target.transform.position = new Vector2(5f, 0f);
        Physics2D.SyncTransforms();
        feature.OnUpdate(0.3f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void SameWeaponTypeStatesAreIndependent()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 1f);
        Weapon firstWeapon = CreateWeapon("knife", Vector2.zero, 2f);
        Weapon secondWeapon = CreateWeapon("knife", new Vector2(5f, 0f), 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, firstWeapon);
        AddEquippedWeapon(fixture.WeaponsHolder, secondWeapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity firstTarget = CreateTarget("first_target", new Vector2(1f, 0f));
        TestEntity secondTarget = CreateTarget("second_target", new Vector2(6f, 0f));

        feature.OnUpdate(0.1f);
        firstWeapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, firstTarget, firstWeapon));
        secondWeapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, secondTarget, secondWeapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(20f).Within(0.0001f));
    }

    [Test]
    public void CooldownPreventsRepeatedTriggerUntilElapsed()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 1f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));
        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(10f).Within(0.0001f));

        feature.OnUpdate(1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(20f).Within(0.0001f));
    }

    [Test]
    public void MaxStacksCapsMaxHealthModifier()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 2,
            maxStacks: 3,
            maxHealthPerStack: 5f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));

        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));
        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));
        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(15f).Within(0.0001f));
    }

    [Test]
    public void MultipleCollidersOnSameTargetDoNotDoubleCountDwell()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 1.5f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f), addDuplicateCollider: true);

        feature.OnUpdate(1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void UninstallRemovesAppliedModifiers()
    {
        TestFixture fixture = CreateFixture();
        WeaponTypeRangeDwellMaxHealthFeature feature = InstallFeature(
            fixture,
            requiredDwellSeconds: 0.1f,
            lingerSeconds: 0.5f,
            stacksPerTrigger: 1,
            maxStacks: 5,
            maxHealthPerStack: 10f,
            cooldownSeconds: 0f);
        Weapon weapon = CreateWeapon("knife", Vector2.zero, 2f);
        AddEquippedWeapon(fixture.WeaponsHolder, weapon);
        RaiseWeaponsChanged(fixture.WeaponsHolder);
        TestEntity target = CreateTarget("target", new Vector2(1f, 0f));
        feature.OnUpdate(0.1f);
        weapon.NotifyDamageDealt(CreateHitResult(fixture.Owner, target, weapon));

        feature.OnUninstall();

        Assert.That(fixture.PropertiesManager.GetPropValue(PropType.MaxHealth), Is.EqualTo(0f).Within(0.0001f));
    }

    private TestFixture CreateFixture()
    {
        GameObject gameObject = CreateGameObject("feature_owner");
        TestEntity owner = gameObject.AddComponent<TestEntity>();
        PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();
        WeaponsHolder weaponsHolder = gameObject.AddComponent<WeaponsHolder>();
        propertiesManager.Initialize(owner);
        return new TestFixture(owner, propertiesManager, weaponsHolder);
    }

    private WeaponTypeRangeDwellMaxHealthFeature InstallFeature(
        TestFixture fixture,
        float requiredDwellSeconds,
        float lingerSeconds,
        int stacksPerTrigger,
        int maxStacks,
        float maxHealthPerStack,
        float cooldownSeconds)
    {
        WeaponTypeRangeDwellMaxHealthFeature feature = new(
            "knife",
            requiredDwellSeconds,
            lingerSeconds,
            stacksPerTrigger,
            maxStacks,
            maxHealthPerStack,
            cooldownSeconds)
        {
            Context = new FeatureContext(fixture.Owner, fixture.PropertiesManager),
            SourceId = "TestFeature"
        };
        feature.OnInstall();
        return feature;
    }

    private Weapon CreateWeapon(string weaponId, Vector2 position, float range)
    {
        GameObject gameObject = CreateGameObject($"weapon_{weaponId}");
        gameObject.transform.position = position;
        Weapon weapon = gameObject.AddComponent<Weapon>();
        WeaponDataSO weaponData = ScriptableObject.CreateInstance<WeaponDataSO>();
        createdObjects.Add(weaponData);
        SetPrivateField(weaponData, "weaponId", weaponId);
        SetBackingField(weapon, nameof(Weapon.WeaponData), weaponData);
        SetBackingField(weapon, nameof(Weapon.Range), range);
        weapon.SetTargetLayerMask(1 << TargetLayer);
        return weapon;
    }

    private TestEntity CreateTarget(string name, Vector2 position, bool addDuplicateCollider = false)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.layer = TargetLayer;
        gameObject.transform.position = position;
        TestEntity target = gameObject.AddComponent<TestEntity>();
        gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
        if (addDuplicateCollider)
        {
            gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.2f;
        }

        gameObject.AddComponent<HealthComponent>();
        Physics2D.SyncTransforms();
        return target;
    }

    private HitResult CreateHitResult(Entity source, Entity target, Weapon weapon)
    {
        return new HitResult(
            source,
            target,
            1f,
            target.Center,
            false,
            false,
            false,
            false,
            HitSourceKind.Weapon,
            weapon.transform.position,
            weapon);
    }

    private void AddEquippedWeapon(WeaponsHolder weaponsHolder, Weapon weapon)
    {
        List<EquippedWeaponInfo> equippedWeapons = GetPrivateField<List<EquippedWeaponInfo>>(weaponsHolder, "equippedWeapons");
        equippedWeapons.Add(new EquippedWeaponInfo(weapon.WeaponData, weapon.Level, weapon));
    }

    private static void RaiseWeaponsChanged(WeaponsHolder weaponsHolder)
    {
        FieldInfo eventField = typeof(WeaponsHolder).GetField("OnWeaponsChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(eventField, $"Missing event field '{nameof(WeaponsHolder.OnWeaponsChanged)}'.");
        (eventField.GetValue(weaponsHolder) as Action)?.Invoke();
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetBackingField<TTarget>(TTarget target, string propertyName, object value)
    {
        SetPrivateField(target, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field ??= typeof(TTarget).BaseType?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }

    private static TField GetPrivateField<TField>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field ??= target.GetType().BaseType?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
        return (TField)field.GetValue(target);
    }

    private readonly struct TestFixture
    {
        public TestFixture(TestEntity owner, PropertiesManager propertiesManager, WeaponsHolder weaponsHolder)
        {
            Owner = owner;
            PropertiesManager = propertiesManager;
            WeaponsHolder = weaponsHolder;
        }

        public TestEntity Owner { get; }
        public PropertiesManager PropertiesManager { get; }
        public WeaponsHolder WeaponsHolder { get; }
    }

    private sealed class TestEntity : Entity
    {
    }
}
