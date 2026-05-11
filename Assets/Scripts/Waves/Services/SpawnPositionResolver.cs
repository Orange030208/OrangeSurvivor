using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPositionResolver
{
    private const float FALLBACK_OCCUPANCY_RADIUS = 0.5f;
    private const int OBSTACLE_HIT_BUFFER_SIZE = 8;

    private static bool hasLoggedMissingObstacleLayer;
    private static bool hasLoggedFallbackCollider;

    private readonly SpawnLocationPolicyType policyType;
    private readonly float minDistance;
    private readonly float maxDistance;
    private readonly float boundsPadding;
    private readonly int resolveAttempts;
    private readonly LayerMask obstacleLayerMask;
    private readonly float spawnClearance;
    private readonly Vector2 minBounds;
    private readonly Vector2 maxBounds;
    private readonly Collider2D[] obstacleHitBuffer = new Collider2D[OBSTACLE_HIT_BUFFER_SIZE];

    private SpawnPositionResolver(
        SpawnLocationPolicyType policyType,
        float minDistance,
        float maxDistance,
        float boundsPadding,
        int resolveAttempts,
        LayerMask obstacleLayerMask,
        float spawnClearance,
        Vector2 minBounds,
        Vector2 maxBounds)
    {
        this.policyType = policyType;
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
        this.boundsPadding = boundsPadding;
        this.resolveAttempts = resolveAttempts;
        this.obstacleLayerMask = obstacleLayerMask;
        this.spawnClearance = spawnClearance;
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
    }

    public static SpawnPositionResolver FromPolicy(SpawnLocationPolicySO policy)
    {
        if (policy == null)
        {
            throw new MissingReferenceException($"{nameof(SpawnLocationPolicySO)} is required for wave spawning.");
        }

        return new SpawnPositionResolver(
            policy.PolicyType,
            policy.MinDistance,
            policy.MaxDistance,
            policy.BoundsPadding,
            policy.ResolveAttempts,
            policy.ObstacleLayerMask,
            policy.SpawnClearance,
            policy.MinBounds,
            policy.MaxBounds);
    }

    public Vector3 Resolve(SpawnContext context)
    {
        if (TryResolve(context, null, out Vector3 position))
        {
            return position;
        }

        throw new MissingReferenceException($"{nameof(SpawnPositionResolver)} could not resolve a valid spawn position.");
    }

    public bool TryResolve(SpawnContext context, EnemySO enemyDefinition, out Vector3 position)
    {
        if (context.AnchorEntity == null)
        {
            throw new MissingReferenceException($"{nameof(SpawnContext)} is missing anchor entity.");
        }

        Vector2 resolvedMinBounds = minBounds;
        Vector2 resolvedMaxBounds = maxBounds;
        if (MapGenerator.TryGetRuntimeBounds(out Bounds runtimeBounds))
        {
            Vector3 extents = runtimeBounds.extents;
            resolvedMinBounds = new Vector2(runtimeBounds.center.x - extents.x, runtimeBounds.center.y - extents.y);
            resolvedMaxBounds = new Vector2(runtimeBounds.center.x + extents.x, runtimeBounds.center.y + extents.y);
        }

        ApplyBoundsPadding(ref resolvedMinBounds, ref resolvedMaxBounds);

        float occupancyRadius = ResolveOccupancyRadius(enemyDefinition);
        for (int i = 0; i < resolveAttempts; i++)
        {
            Vector2 candidate = CreateCandidatePosition(context.AnchorEntity.Center, resolvedMinBounds, resolvedMaxBounds);
            if (IsSafeSpawnPosition(candidate, occupancyRadius, resolvedMinBounds, resolvedMaxBounds))
            {
                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    private Vector2 CreateCandidatePosition(Vector2 anchorPosition, Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        return policyType switch
        {
            SpawnLocationPolicyType.RandomInsideMap => CreateRandomInsideMapPosition(resolvedMinBounds, resolvedMaxBounds),
            SpawnLocationPolicyType.RandomMapEdge => CreateRandomMapEdgePosition(resolvedMinBounds, resolvedMaxBounds),
            _ => CreateBoundedRingPosition(anchorPosition, resolvedMinBounds, resolvedMaxBounds)
        };
    }

    private Vector2 CreateBoundedRingPosition(Vector2 anchorPosition, Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        return ClampInsideBounds(CreateRingPosition(anchorPosition), resolvedMinBounds, resolvedMaxBounds);
    }

    private static Vector2 CreateRandomInsideMapPosition(Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        return new Vector2(
            Random.Range(resolvedMinBounds.x, resolvedMaxBounds.x),
            Random.Range(resolvedMinBounds.y, resolvedMaxBounds.y));
    }

    private static Vector2 CreateRandomMapEdgePosition(Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        int edgeIndex = Random.Range(0, 4);
        return edgeIndex switch
        {
            0 => new Vector2(Random.Range(resolvedMinBounds.x, resolvedMaxBounds.x), resolvedMaxBounds.y),
            1 => new Vector2(Random.Range(resolvedMinBounds.x, resolvedMaxBounds.x), resolvedMinBounds.y),
            2 => new Vector2(resolvedMinBounds.x, Random.Range(resolvedMinBounds.y, resolvedMaxBounds.y)),
            _ => new Vector2(resolvedMaxBounds.x, Random.Range(resolvedMinBounds.y, resolvedMaxBounds.y))
        };
    }

    private Vector2 CreateRingPosition(Vector2 anchorPosition)
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.up;
        }

        float spawnDistance = Random.Range(minDistance, maxDistance);
        return anchorPosition + direction * spawnDistance;
    }

    private void ApplyBoundsPadding(ref Vector2 resolvedMinBounds, ref Vector2 resolvedMaxBounds)
    {
        float safePaddingX = Mathf.Min(boundsPadding, Mathf.Max(0f, (resolvedMaxBounds.x - resolvedMinBounds.x) * 0.5f));
        float safePaddingY = Mathf.Min(boundsPadding, Mathf.Max(0f, (resolvedMaxBounds.y - resolvedMinBounds.y) * 0.5f));
        resolvedMinBounds += new Vector2(safePaddingX, safePaddingY);
        resolvedMaxBounds -= new Vector2(safePaddingX, safePaddingY);
    }

    private bool IsSafeSpawnPosition(
        Vector2 position,
        float occupancyRadius,
        Vector2 resolvedMinBounds,
        Vector2 resolvedMaxBounds)
    {
        if (!IsInsideBounds(position, resolvedMinBounds, resolvedMaxBounds))
        {
            return false;
        }

        int obstacleMask = obstacleLayerMask.value;
        if (obstacleMask == 0)
        {
            if (!hasLoggedMissingObstacleLayer)
            {
                Debug.LogWarning($"[{nameof(SpawnPositionResolver)}] No obstacle layer mask is configured. Spawn wall checks will be skipped.");
                hasLoggedMissingObstacleLayer = true;
            }

            return true;
        }

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            position,
            occupancyRadius,
            obstacleHitBuffer,
            obstacleMask);
        return hitCount <= 0;
    }

    private float ResolveOccupancyRadius(EnemySO enemyDefinition)
    {
        if (enemyDefinition != null && enemyDefinition.prefab != null)
        {
            Collider2D entityCollider = enemyDefinition.prefab.EntityCollider;
            if (TryResolveColliderRadius(entityCollider, out float colliderRadius))
            {
                return colliderRadius + spawnClearance;
            }
        }

        if (!hasLoggedFallbackCollider)
        {
            string enemyName = enemyDefinition != null ? enemyDefinition.name : "unknown";
            Debug.LogWarning($"[{nameof(SpawnPositionResolver)}] Enemy '{enemyName}' has no supported root Collider2D for spawn occupancy checks. Using fallback radius {FALLBACK_OCCUPANCY_RADIUS:0.###}.");
            hasLoggedFallbackCollider = true;
        }

        return FALLBACK_OCCUPANCY_RADIUS + spawnClearance;
    }

    private static bool TryResolveColliderRadius(Collider2D entityCollider, out float radius)
    {
        radius = 0f;
        if (entityCollider == null)
        {
            return false;
        }

        Vector2 scale = entityCollider.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        switch (entityCollider)
        {
            case CircleCollider2D circleCollider:
                radius = circleCollider.radius * maxScale;
                return radius > 0f;

            case BoxCollider2D boxCollider:
                radius = Mathf.Max(boxCollider.size.x, boxCollider.size.y) * 0.5f * maxScale;
                return radius > 0f;

            case CapsuleCollider2D capsuleCollider:
                radius = Mathf.Max(capsuleCollider.size.x, capsuleCollider.size.y) * 0.5f * maxScale;
                return radius > 0f;

            default:
                return false;
        }
    }

    private static bool IsInsideBounds(Vector2 position, Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        return position.x >= resolvedMinBounds.x
            && position.x <= resolvedMaxBounds.x
            && position.y >= resolvedMinBounds.y
            && position.y <= resolvedMaxBounds.y;
    }

    private static Vector2 ClampInsideBounds(Vector2 position, Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        position.x = Mathf.Clamp(position.x, resolvedMinBounds.x, resolvedMaxBounds.x);
        position.y = Mathf.Clamp(position.y, resolvedMinBounds.y, resolvedMaxBounds.y);
        return position;
    }
}
