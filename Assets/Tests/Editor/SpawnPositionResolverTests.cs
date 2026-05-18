using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SpawnPositionResolverTests
{
    private GameObject anchorObject;
    private GameObject wallObject;

    [SetUp]
    public void SetUp()
    {
        SetMapGeneratorRuntimeBoundsState(false);
        SetSpawnPositionResolverStaticFlag("hasLoggedFallbackCollider", false);
    }

    [TearDown]
    public void TearDown()
    {
        if (anchorObject != null)
        {
            Object.DestroyImmediate(anchorObject);
        }

        if (wallObject != null)
        {
            Object.DestroyImmediate(wallObject);
        }

        SetMapGeneratorRuntimeBoundsState(false);
    }

    [TestCase(nameof(AroundPlayerRingSpawnLocationStrategy))]
    [TestCase(nameof(RandomInsideMapSpawnLocationStrategy))]
    [TestCase(nameof(RandomMapEdgeSpawnLocationStrategy))]
    public void TryResolveRejectsPositionsOverlappingObstacleLayer(string strategyType)
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        Assert.GreaterOrEqual(wallLayer, 0, "Project must define the Wall layer for spawn obstacle checks.");

        SpawnLocationDefinition spawnLocation = CreateSpawnLocation(strategyType);
        SpawnPositionResolver resolver = SpawnPositionResolver.FromDefinition(spawnLocation);
        FakeEntity anchor = CreateAnchor();
        CreateBlockingWall(wallLayer);

        ExpectFallbackColliderWarning();
        bool resolved = resolver.TryResolve(new SpawnContext(anchor, 0f, 0), null, out _);

        Assert.IsFalse(resolved);
    }

    [Test]
    public void TryResolveSucceedsWhenNoObstacleOverlapsCandidate()
    {
        SpawnLocationDefinition spawnLocation = CreateSpawnLocation(nameof(RandomInsideMapSpawnLocationStrategy));
        SpawnPositionResolver resolver = SpawnPositionResolver.FromDefinition(spawnLocation);
        FakeEntity anchor = CreateAnchor();

        ExpectFallbackColliderWarning();
        bool resolved = resolver.TryResolve(new SpawnContext(anchor, 0f, 0), null, out Vector3 position);

        Assert.IsTrue(resolved);
        Assert.That(position.x, Is.InRange(-1f, 1f));
        Assert.That(position.y, Is.InRange(-1f, 1f));
    }

    private FakeEntity CreateAnchor()
    {
        anchorObject = new GameObject("Spawn Resolver Test Anchor");
        return new FakeEntity(anchorObject.transform);
    }

    private void CreateBlockingWall(int wallLayer)
    {
        wallObject = new GameObject("Spawn Resolver Test Wall");
        wallObject.layer = wallLayer;
        BoxCollider2D wallCollider = wallObject.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(8f, 8f);
        wallObject.transform.position = Vector3.zero;
        Physics2D.SyncTransforms();
    }

    private static SpawnLocationDefinition CreateSpawnLocation(string strategyType)
    {
        string[] obstacleLayerNames = { "Wall" };
        Vector2 minBounds = new(-1f, -1f);
        Vector2 maxBounds = new(1f, 1f);
        SpawnLocationResolverSettings settings = new(
            0f,
            4,
            0f,
            minBounds,
            maxBounds,
            obstacleLayerNames);
        SpawnLocationStrategyModel strategy = strategyType switch
        {
            nameof(AroundPlayerRingSpawnLocationStrategy) => new AroundPlayerRingSpawnLocationStrategy(
                0.1f,
                0.2f),
            nameof(RandomInsideMapSpawnLocationStrategy) => new RandomInsideMapSpawnLocationStrategy(),
            nameof(RandomMapEdgeSpawnLocationStrategy) => new RandomMapEdgeSpawnLocationStrategy(),
            _ => throw new AssertionException($"Unsupported spawn location strategy type '{strategyType}'.")
        };

        return new SpawnLocationDefinition(settings, strategy);
    }

    private static void SetMapGeneratorRuntimeBoundsState(bool hasRuntimeBounds)
    {
        FieldInfo field = typeof(MapGenerator).GetField(
            "hasRuntimeBounds",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field hasRuntimeBounds on {nameof(MapGenerator)}.");
        field.SetValue(null, hasRuntimeBounds);
    }

    private static void SetSpawnPositionResolverStaticFlag(string fieldName, bool value)
    {
        FieldInfo field = typeof(SpawnPositionResolver).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field {fieldName} on {nameof(SpawnPositionResolver)}.");
        field.SetValue(null, value);
    }

    private static void ExpectFallbackColliderWarning()
    {
        LogAssert.Expect(
            LogType.Warning,
            $"[{nameof(SpawnPositionResolver)}] Enemy 'unknown' has no supported root Collider2D for spawn occupancy checks. Using fallback radius 0.5.");
    }

    private sealed class FakeEntity : IEntity
    {
        public FakeEntity(Transform transform)
        {
            Transform = transform;
        }

        public Collider2D EntityCollider => null;
        public Transform Transform { get; }
        public Vector2 Center => Transform.position;
        public EntityRenderer EntityRenderer => null;
        public bool IsRuntimeEnabled => true;
        public string RuntimeId => "FakeEntity";

        public void EnableRuntime()
        {
        }

        public void DisableRuntime()
        {
        }
    }
}
