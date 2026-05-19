using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EnemyMeleeSemicircleTests
{
    private const string SKELETON_PREFAB_PATH = "Assets/GameContent/Enemies/Prefabs/Skeleton.prefab";
    private const string EVIL_SLIME_PREFAB_PATH = "Assets/GameContent/Enemies/Prefabs/Evil Slime.prefab";
    private const string SKELETON_METEORHAMMER_PREFAB_PATH = "Assets/GameContent/Enemies/Prefabs/Skeleton Meteorhammer.prefab";
    private const string SKELETON_METEORHAMMER2_PREFAB_PATH = "Assets/GameContent/Enemies/Prefabs/Skeleton Meteorhammer2.prefab";
    private const string SKELETON_METEORHAMMER2_ENEMY_PATH = "Assets/GameContent/Enemies/Data/Skeleton Meteorhammer2/SkeletonMeteorhammer2Enemy.asset";
    private const string SKELETON_METEORHAMMER2_CONTROLLER_PATH = "Assets/GameContent/Enemies/Animations/Skeleton Meteorhammer2/Skeleton Meteorhammer2.controller";
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
    public void DirectDamageAttackStrategyUsesAttackRangeFacingSemicircle()
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
            hitShape: DirectDamageHitShape.FacingSemicircle,
            rangeDirectionProvider: ResolveHorizontalDirectionToTarget);
        TestEntity frontTarget = CreateTarget("front_target", new Vector2(1.2f, 0f));
        TestEntity farTarget = CreateTarget("far_target", new Vector2(2.0f, 0f));
        TestEntity behindAttackPointTarget = CreateTarget("behind_attack_point_target", new Vector2(0.2f, 0f));
        Physics2D.SyncTransforms();

        Assert.IsTrue(strategy.IsTargetInRange(frontTarget));
        Assert.IsFalse(strategy.IsTargetInRange(farTarget));
        Assert.IsFalse(strategy.IsTargetInRange(behindAttackPointTarget));
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

    [TestCase(SKELETON_PREFAB_PATH, typeof(SkeletonBrain))]
    [TestCase(EVIL_SLIME_PREFAB_PATH, typeof(SkeletonBrain))]
    [TestCase(SKELETON_METEORHAMMER_PREFAB_PATH, typeof(SkeletonMeteorhammerBrain))]
    [TestCase(SKELETON_METEORHAMMER2_PREFAB_PATH, typeof(SkeletonMeteorhammer2Brain))]
    public void SkeletonPrefabsBindForwardMeleePoint(string prefabPath, System.Type brainType)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Component brain = root.GetComponent(brainType);
            Assert.NotNull(brain, $"{prefabPath} must contain {brainType.Name}.");

            SerializedObject serializedObject = new(brain);
            SerializedProperty property = serializedObject.FindProperty("meleePointTransform");
            Assert.NotNull(property, $"{brainType.Name} must serialize meleePointTransform.");
            Assert.NotNull(property.objectReferenceValue, $"{prefabPath} must bind meleePointTransform.");

            Transform meleePoint = (Transform)property.objectReferenceValue;
            Assert.Greater(meleePoint.localPosition.x, 0f, $"{prefabPath} melee point must be in front when default facing is right.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void SkeletonMeteorhammer2EnemyAssetReferencesMeteorhammer2PrefabAndController()
    {
        SkeletonMeteorhammer2EnemySO enemyData = AssetDatabase.LoadAssetAtPath<SkeletonMeteorhammer2EnemySO>(SKELETON_METEORHAMMER2_ENEMY_PATH);
        Assert.NotNull(enemyData);
        Assert.NotNull(enemyData.prefab);
        Assert.NotNull(enemyData.AnimConfig);
        Assert.NotNull(enemyData.AnimConfig.AnimatorController);

        Assert.AreEqual(SKELETON_METEORHAMMER2_PREFAB_PATH, AssetDatabase.GetAssetPath(enemyData.prefab));
        Assert.AreEqual(SKELETON_METEORHAMMER2_CONTROLLER_PATH, AssetDatabase.GetAssetPath(enemyData.AnimConfig.AnimatorController));
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

    private static Vector2 ResolveHorizontalDirectionToTarget(Entity target)
    {
        return target != null && target.Center.x < 0f ? Vector2.left : Vector2.right;
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
