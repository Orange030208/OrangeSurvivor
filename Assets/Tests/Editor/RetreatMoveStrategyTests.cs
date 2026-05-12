using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RetreatMoveStrategyTests
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
    public void ExecuteMoveScalesRetreatTargetDistanceByDetectionRange()
    {
        TestEnemy owner = CreateEnemy("owner", Vector2.zero, 500f);
        TestEntity target = CreateEntity("target", new Vector2(2f, 0f));
        TestMovable movable = new(owner.PropertiesManager);
        RetreatMoveStrategy strategy = new(owner, movable, owner.PropertiesManager, new RetreatMoveData
        {
            safeDistanceRatio = 1f,
            retreatStepDistanceRatio = 0.4f
        });

        strategy.ExecuteMove(target);

        Assert.AreEqual(1, movable.MoveToCallCount);
        Assert.AreEqual(0, movable.StopMovingCallCount);
        Assert.That(movable.LastMoveTarget.x, Is.EqualTo(-2f).Within(0.0001f));
        Assert.That(movable.LastMoveTarget.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void ExecuteMoveScalesSafeDistanceByDetectionRange()
    {
        TestEnemy owner = CreateEnemy("owner", Vector2.zero, 500f);
        TestEntity target = CreateEntity("target", new Vector2(1.5f, 0f));
        TestMovable movable = new(owner.PropertiesManager);
        RetreatMoveStrategy strategy = new(owner, movable, owner.PropertiesManager, new RetreatMoveData
        {
            safeDistanceRatio = 0.4f,
            retreatStepDistanceRatio = 0.2f
        });

        strategy.ExecuteMove(target);

        Assert.AreEqual(1, movable.MoveToCallCount);
        Assert.AreEqual(0, movable.StopMovingCallCount);
        Assert.That(movable.LastMoveTarget.x, Is.EqualTo(-1f).Within(0.0001f));
        Assert.That(movable.LastMoveTarget.y, Is.EqualTo(0f).Within(0.0001f));
    }

    private TestEnemy CreateEnemy(string name, Vector2 position, float detectionRangePoints)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.transform.position = position;
        TestEnemy enemy = gameObject.AddComponent<TestEnemy>();
        PropertiesManager propertiesManager = gameObject.AddComponent<PropertiesManager>();
        enemy.Configure(propertiesManager);
        SetCalculatedProps(propertiesManager, new[]
        {
            new BasePropData(PropType.DetectionRange, detectionRangePoints)
        });
        return enemy;
    }

    private TestEntity CreateEntity(string name, Vector2 position)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.transform.position = position;
        return gameObject.AddComponent<TestEntity>();
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetCalculatedProps(PropertiesManager manager, IReadOnlyList<BasePropData> values)
    {
        FieldInfo field = typeof(PropertiesManager).GetField("calculatedProps", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field 'calculatedProps' on {nameof(PropertiesManager)}.");
        Dictionary<PropType, float> calculatedProps = (Dictionary<PropType, float>)field.GetValue(manager);
        calculatedProps.Clear();
        for (int i = 0; i < values.Count; i++)
        {
            calculatedProps[values[i].propType] = values[i].value;
        }
    }

    private sealed class TestEntity : Entity
    {
    }

    private sealed class TestEnemy : Enemy
    {
        public PropertiesManager PropertiesManager { get; private set; }

        public void Configure(PropertiesManager propertiesManager)
        {
            PropertiesManager = propertiesManager;
        }
    }

    private sealed class TestMovable : IMovable
    {
        public TestMovable(PropertiesManager propertiesManager)
        {
            PropertiesManager = propertiesManager;
        }

        public int MoveToCallCount { get; private set; }
        public int StopMovingCallCount { get; private set; }
        public Vector2 LastMoveTarget { get; private set; }

        public float Speed => 0f;
        public Vector2 MoveDirection => Vector2.zero;
        public bool IsMoving => false;
        public PropertiesManager PropertiesManager { get; }

        public void EnableMovement()
        {
        }

        public void DisableMovement()
        {
        }

        public void MoveTo(Vector2 position)
        {
            MoveToCallCount++;
            LastMoveTarget = position;
        }

        public void StopMoving()
        {
            StopMovingCallCount++;
        }
    }
}
