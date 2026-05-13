using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EntityDeathReasonTests
{
    private readonly System.Collections.Generic.List<Object> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        GameEventBus.Clear();
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
    public void EntityDiedEventDefaultsToCombatReason()
    {
        EntityDiedEvent diedEvent = new(null, Vector2.zero);

        Assert.AreEqual(EntityDeathReason.Combat, diedEvent.Reason);
    }

    [Test]
    public void EntityDiedEventPreservesExplicitReason()
    {
        EntityDiedEvent diedEvent = new(null, Vector2.zero, null, EntityDeathReason.WaveCleanup);

        Assert.AreEqual(EntityDeathReason.WaveCleanup, diedEvent.Reason);
    }

    [Test]
    public void ForceDeathPublishesWaveCleanupReason()
    {
        CreateEntityWithHealth(out HealthComponent healthComponent);
        healthComponent.OnDeathSequenceRequested += HoldDeathSequenceForTest;
        EntityDiedEvent capturedEvent = default;
        bool eventReceived = false;
        GameEventBus.Subscribe<EntityDiedEvent>(diedEvent =>
        {
            capturedEvent = diedEvent;
            eventReceived = true;
        });

        bool started = healthComponent.ForceDeath(null, EntityDeathReason.WaveCleanup);

        Assert.IsTrue(started);
        Assert.IsTrue(eventReceived);
        Assert.AreEqual(EntityDeathReason.WaveCleanup, capturedEvent.Reason);
    }

    [Test]
    public void EnemyDefeatForWaveEndDoesNotRestartRunningDeathSequence()
    {
        TestEnemy enemy = CreateEnemyWithHealth(out HealthComponent healthComponent);
        SetPrivateField(healthComponent, "isDeathSequenceRunning", true);
        int deathEventCount = 0;
        GameEventBus.Subscribe<EntityDiedEvent>(_ => deathEventCount++);

        bool waveEndStarted = enemy.DefeatForWaveEnd();

        Assert.IsTrue(waveEndStarted);
        Assert.AreEqual(0, deathEventCount);
        Assert.IsTrue(healthComponent.IsDeathSequenceRunning);
    }

    private TestEntity CreateEntityWithHealth(out HealthComponent healthComponent)
    {
        GameObject gameObject = CreateGameObject("entity_death_reason_test");
        TestEntity entity = gameObject.AddComponent<TestEntity>();
        healthComponent = gameObject.AddComponent<HealthComponent>();
        healthComponent.Initialize(entity);
        return entity;
    }

    private TestEnemy CreateEnemyWithHealth(out HealthComponent healthComponent)
    {
        GameObject gameObject = CreateGameObject("enemy_death_reason_test");
        gameObject.AddComponent<TestAnimatable>();
        healthComponent = gameObject.AddComponent<HealthComponent>();
        gameObject.AddComponent<Rigidbody2D>();
        gameObject.AddComponent<BoxCollider2D>();
        gameObject.AddComponent<PropertiesManager>();
        TestEnemy enemy = gameObject.AddComponent<TestEnemy>();
        enemy.AssignHealthComponent(healthComponent);
        return enemy;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = FindPrivateField(target.GetType(), fieldName);
        Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static System.Reflection.FieldInfo FindPrivateField(System.Type type, string fieldName)
    {
        while (type != null)
        {
            System.Reflection.FieldInfo field = type.GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static IEnumerator HoldDeathSequenceForTest()
    {
        yield return null;
    }

    private sealed class TestEntity : Entity
    {
    }

    private sealed class TestEnemy : Enemy
    {
        public void AssignHealthComponent(HealthComponent healthComponent)
        {
            SetPrivateField(this, "healthComponent", healthComponent);
        }
    }

    private sealed class TestAnimatable : MonoBehaviour, IAnimatable
    {
        public void SetBool(int id, bool value) { }
        public void SetTrigger(int id) { }
        public void SetFloat(int id, float value) { }
        public void SetInteger(int id, int value) { }
        public void SetBool(string paramName, bool value) { }
        public void SetTrigger(string paramName) { }
        public void SetFloat(string paramName, float value) { }
        public void SetInteger(string paramName, int value) { }
        public void PlayState(string stateName) { }
        public void PlayState(int stateHash) { }
        public void PlayState(int stateHash, float normalizedTime, int layerIndex = 0) { }
        public void SetPlaybackSpeed(float speed) { }
        public void ResetPlaybackSpeed() { }
        public bool IsCurrentState(int stateHash, int layerIndex = 0) => false;
        public float GetCurrentStateNormalizedTime(int layerIndex = 0) => 0f;
        public AnimationStateProgress GetStateProgress(int stateHash, int layerIndex = 0) => default;
    }
}
