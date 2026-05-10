using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class WeaponMeleeStabilizationTests
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
    public void FixedSequenceTimingUsesFullSequenceDuration()
    {
        TestWeapon weapon = CreateWeapon(WeaponAttackTimingMode.FixedSequenceThenCooldown);
        AttackSequenceDefinitionSO sequence = CreateSequence(1.25f);
        SetBackingField(weapon, nameof(Weapon.AttackInterval), 0.2f);

        float duration = weapon.ResolveDurationForTest(sequence);

        Assert.That(duration, Is.EqualTo(1.25f).Within(0.0001f));
    }

    [Test]
    public void CompressedTimingPreservesOldDurationClamp()
    {
        TestWeapon weapon = CreateWeapon(WeaponAttackTimingMode.CompressedIntoAttackInterval, occupancy: 0.5f);
        AttackSequenceDefinitionSO sequence = CreateSequence(1f);
        SetBackingField(weapon, nameof(Weapon.AttackInterval), 0.4f);

        float duration = weapon.ResolveDurationForTest(sequence);

        Assert.That(duration, Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    public void FixedSequenceCompletionStartsFullCooldown()
    {
        TestWeapon weapon = CreateWeapon(WeaponAttackTimingMode.FixedSequenceThenCooldown);
        SetBackingField(weapon, nameof(Weapon.AttackInterval), 0.7f);
        SetPrivateField(weapon, "cooldownRemaining", 0f);

        InvokePrivateMethod(weapon, "FinishAttackSequence");

        Assert.That(GetPrivateField<float>(weapon, "cooldownRemaining"), Is.EqualTo(0.7f).Within(0.0001f));
    }

    [Test]
    public void FixedSequenceCooldownDoesNotTickOnCompletionFrame()
    {
        TestWeapon weapon = CreateWeapon(WeaponAttackTimingMode.FixedSequenceThenCooldown);
        SetBackingField(weapon, nameof(Weapon.AttackInterval), 0.7f);
        SetPrivateField(weapon, "cooldownRemaining", 0f);

        InvokePrivateMethod(weapon, "FinishAttackSequence");
        InvokePrivateMethod(weapon, "TickCooldown", 0.1f);

        Assert.That(GetPrivateField<float>(weapon, "cooldownRemaining"), Is.EqualTo(0.7f).Within(0.0001f));
    }

    [Test]
    public void CompressedSequenceCompletionDoesNotRestartCooldown()
    {
        TestWeapon weapon = CreateWeapon(WeaponAttackTimingMode.CompressedIntoAttackInterval);
        SetBackingField(weapon, nameof(Weapon.AttackInterval), 0.7f);
        SetPrivateField(weapon, "cooldownRemaining", 0.25f);

        InvokePrivateMethod(weapon, "FinishAttackSequence");

        Assert.That(GetPrivateField<float>(weapon, "cooldownRemaining"), Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void StableLockKeepsCurrentTargetAgainstSlightlyCloserCandidate()
    {
        WeaponTargetSelector selector = new();
        TestTarget current = CreateTarget("current", new Vector2(0f, 1f));
        CreateTarget("candidate", new Vector2(0f, 0.95f));
        Physics2D.SyncTransforms();

        Entity selected = selector.SelectTarget(
            current,
            Vector2.zero,
            Vector2.up,
            2f,
            Physics2D.DefaultRaycastLayers,
            WeaponTargetingMode.StableLock);

        Assert.AreSame(current, selected);
    }

    [Test]
    public void StableLockSwitchesToClearlyBetterCandidate()
    {
        WeaponTargetSelector selector = new();
        TestTarget current = CreateTarget("current", new Vector2(0f, 1.4f));
        TestTarget candidate = CreateTarget("candidate", new Vector2(0f, 0.2f));
        Physics2D.SyncTransforms();

        Entity selected = selector.SelectTarget(
            current,
            Vector2.zero,
            Vector2.up,
            2f,
            Physics2D.DefaultRaycastLayers,
            WeaponTargetingMode.StableLock);

        Assert.AreSame(candidate, selected);
    }

    [Test]
    public void StableLockUsesProvidedWeaponOrigin()
    {
        WeaponTargetSelector selector = new();
        TestTarget nearWorldOrigin = CreateTarget("near_world_origin", Vector2.zero);
        TestTarget nearWeaponOrigin = CreateTarget("near_weapon_origin", new Vector2(5f, 0f));
        Physics2D.SyncTransforms();

        Entity selected = selector.SelectTarget(
            null,
            new Vector2(5f, 0f),
            Vector2.right,
            1f,
            Physics2D.DefaultRaycastLayers,
            WeaponTargetingMode.StableLock);

        Assert.AreSame(nearWeaponOrigin, selected);
        Assert.AreNotSame(nearWorldOrigin, selected);
    }

    private TestWeapon CreateWeapon(WeaponAttackTimingMode timingMode, float occupancy = 0.85f)
    {
        GameObject gameObject = CreateGameObject("weapon");
        TestWeapon weapon = gameObject.AddComponent<TestWeapon>();
        WeaponDataSO weaponData = ScriptableObject.CreateInstance<WeaponDataSO>();
        createdObjects.Add(weaponData);
        SetBackingField(weapon, nameof(Weapon.WeaponData), weaponData);
        SetPrivateField(weaponData, "attackTimingMode", timingMode);
        SetPrivateField(weaponData, "attackSequenceOccupancy", occupancy);
        return weapon;
    }

    private AttackSequenceDefinitionSO CreateSequence(float duration)
    {
        AttackSequenceDefinitionSO sequence = ScriptableObject.CreateInstance<AttackSequenceDefinitionSO>();
        createdObjects.Add(sequence);
        SetPrivateField(sequence, "duration", duration);
        return sequence;
    }

    private TestTarget CreateTarget(string name, Vector2 position)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.transform.position = position;
        gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
        HealthComponent healthComponent = gameObject.AddComponent<HealthComponent>();
        SetPrivateField(healthComponent, "maxHealth", 1f);
        SetPrivateField(healthComponent, "health", 1f);
        return gameObject.AddComponent<TestTarget>();
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

    private static void InvokePrivateMethod(object target, string methodName)
    {
        InvokePrivateMethod(target, methodName, null);
    }

    private static void InvokePrivateMethod(object target, string methodName, params object[] parameters)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method ??= target.GetType().BaseType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing private method '{methodName}' on {target.GetType().Name}.");
        method.Invoke(target, parameters);
    }

    private sealed class TestWeapon : Weapon
    {
        public float ResolveDurationForTest(AttackSequenceDefinitionSO sequence)
        {
            return ResolveAttackSequenceDuration(sequence);
        }
    }

    private sealed class TestTarget : Entity
    {
    }
}
