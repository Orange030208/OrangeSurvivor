using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyMeleeCircleTests
{
    private const int TEST_LAYER = 30;
    private const int TEST_LAYER_MASK = 1 << TEST_LAYER;

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
    public void FacingSemicircleIncludesFrontTarget()
    {
        Collider2D[] results = new Collider2D[4];
        TestEntity target = CreateTarget("front_target", new Vector2(0.5f, 0f));
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapFacingSemicircleNonAlloc(
            Vector2.zero,
            1f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(1, hitCount);
        Assert.AreSame(target.EntityCollider, results[0]);
    }

    [Test]
    public void FacingSemicircleExcludesBackTarget()
    {
        Collider2D[] results = new Collider2D[4];
        CreateTarget("back_target", new Vector2(-0.5f, 0f));
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapFacingSemicircleNonAlloc(
            Vector2.zero,
            1f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(0, hitCount);
    }

    [Test]
    public void FacingSemicircleIncludesColliderOverlappingAttackCenter()
    {
        Collider2D[] results = new Collider2D[4];
        TestEntity target = CreateTarget("overlap_target", Vector2.zero);
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapFacingSemicircleNonAlloc(
            Vector2.zero,
            1f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(1, hitCount);
        Assert.AreSame(target.EntityCollider, results[0]);
    }

    [Test]
    public void DirectDamageAttackStrategyUsesAttackRangeCircle()
    {
        TestEnemy owner = CreateEnemy("skeleton_owner", Vector2.zero, new[]
        {
            new BasePropData(PropType.AttackRange, 100f)
        });
        EnemyAttackController attackController = owner.gameObject.AddComponent<EnemyAttackController>();
        Transform attackPoint = CreateAttackPoint(owner.transform, new Vector2(0.5f, 0f));
        DirectDamageAttackStrategy strategy = new(
            owner,
            attackController,
            owner.Properties,
            "test_attack",
            1f,
            attackPoint,
            hitShape: DirectDamageHitShape.Circle);
        TestEntity frontTarget = CreateTarget("front_target", new Vector2(1.2f, 0f));
        TestEntity behindAttackPointTarget = CreateTarget("behind_attack_point_target", new Vector2(0.2f, 0f));
        TestEntity farTarget = CreateTarget("far_target", new Vector2(2.0f, 0f));
        Physics2D.SyncTransforms();

        Assert.IsTrue(strategy.IsTargetInRange(frontTarget));
        Assert.IsTrue(strategy.IsTargetInRange(behindAttackPointTarget));
        Assert.IsFalse(strategy.IsTargetInRange(farTarget));
    }

    [Test]
    public void ForwardBoxIncludesFrontTarget()
    {
        Collider2D[] results = new Collider2D[4];
        TestEntity target = CreateTarget("front_target", new Vector2(1f, 0f));
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapForwardBoxNonAlloc(
            Vector2.zero,
            2f,
            0.4f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(1, hitCount);
        Assert.AreSame(target.EntityCollider, results[0]);
    }

    [Test]
    public void ForwardBoxExcludesBehindTarget()
    {
        Collider2D[] results = new Collider2D[4];
        CreateTarget("behind_target", new Vector2(-0.2f, 0f));
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapForwardBoxNonAlloc(
            Vector2.zero,
            2f,
            0.4f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(0, hitCount);
    }

    [Test]
    public void ForwardBoxExcludesTargetOutsideWidth()
    {
        Collider2D[] results = new Collider2D[4];
        CreateTarget("wide_target", new Vector2(1f, 0.35f));
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapForwardBoxNonAlloc(
            Vector2.zero,
            2f,
            0.4f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(0, hitCount);
    }

    [Test]
    public void ForwardBoxIncludesTargetOverlappingAttackPoint()
    {
        Collider2D[] results = new Collider2D[4];
        TestEntity target = CreateTarget("overlap_target", Vector2.zero);
        Physics2D.SyncTransforms();

        int hitCount = AreaHitQueryUtility.OverlapForwardBoxNonAlloc(
            Vector2.zero,
            2f,
            0.4f,
            Vector2.right,
            results,
            TEST_LAYER_MASK);

        Assert.AreEqual(1, hitCount);
        Assert.AreSame(target.EntityCollider, results[0]);
    }


    private TestEntity CreateTarget(string name, Vector2 position)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.layer = TEST_LAYER;
        gameObject.transform.position = position;
        gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
        return gameObject.AddComponent<TestEntity>();
    }

    private TestEnemy CreateEnemy(string name, Vector2 position, IReadOnlyList<BasePropData> baseProps)
    {
        GameObject gameObject = CreateGameObject(name);
        gameObject.transform.position = position;
        TestEnemy enemy = gameObject.AddComponent<TestEnemy>();
        PropertiesManager propertiesManager = gameObject.GetComponent<PropertiesManager>() ?? gameObject.AddComponent<PropertiesManager>();
        enemy.Configure(propertiesManager);
        SetCalculatedProps(propertiesManager, baseProps);
        return enemy;
    }

    private Transform CreateAttackPoint(Transform parent, Vector2 localPosition)
    {
        GameObject gameObject = CreateGameObject("attack_point");
        Transform attackPoint = gameObject.transform;
        attackPoint.SetParent(parent);
        attackPoint.localPosition = localPosition;
        return attackPoint;
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
        private PropertiesManager properties;

        public PropertiesManager Properties => properties;

        public void Configure(PropertiesManager properties)
        {
            this.properties = properties;
        }
    }
}
