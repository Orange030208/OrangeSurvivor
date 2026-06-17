using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPositionResolver
{
    private const float FALLBACK_OCCUPANCY_RADIUS = 0.5f;
    private const int OBSTACLE_HIT_BUFFER_SIZE = 8;

    private static bool hasLoggedMissingObstacleLayer;
    private static bool hasLoggedFallbackCollider;

    private readonly SpawnLocationResolverSettings settings;
    private readonly Collider2D[] obstacleHitBuffer = new Collider2D[OBSTACLE_HIT_BUFFER_SIZE];

    private SpawnPositionResolver(SpawnLocationDefinition definition)
    {
        SpawnLocationDefinition resolvedDefinition = definition ?? SpawnLocationDefinition.CreateDefault();
        resolvedDefinition.Validate();
        settings = resolvedDefinition.ResolverSettings;
    }

    public static SpawnPositionResolver FromDefinition(SpawnLocationDefinition definition)
    {
        if (definition == null)
        {
            throw new MissingReferenceException($"{nameof(SpawnLocationDefinition)} is required for wave spawning.");
        }

        return new SpawnPositionResolver(definition);
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

        Vector2 resolvedMinBounds = settings.MinBounds;
        Vector2 resolvedMaxBounds = settings.MaxBounds;
        if (MapGenerator.TryGetRuntimeBounds(out Bounds runtimeBounds))
        {
            Vector3 extents = runtimeBounds.extents;
            resolvedMinBounds = new Vector2(runtimeBounds.center.x - extents.x, runtimeBounds.center.y - extents.y);
            resolvedMaxBounds = new Vector2(runtimeBounds.center.x + extents.x, runtimeBounds.center.y + extents.y);
        }

        ApplyBoundsPadding(ref resolvedMinBounds, ref resolvedMaxBounds);

        float occupancyRadius = ResolveOccupancyRadius(enemyDefinition);
        for (int i = 0; i < settings.ResolveAttempts; i++)
        {
            Vector2 candidate = CreateCandidatePosition(resolvedMinBounds, resolvedMaxBounds);
            if (IsSafeSpawnPosition(candidate, occupancyRadius, resolvedMinBounds, resolvedMaxBounds))
            {
                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    private Vector2 CreateCandidatePosition(Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        return new Vector2(
            Random.Range(resolvedMinBounds.x, resolvedMaxBounds.x),
            Random.Range(resolvedMinBounds.y, resolvedMaxBounds.y));
    }

    private void ApplyBoundsPadding(ref Vector2 resolvedMinBounds, ref Vector2 resolvedMaxBounds)
    {
        float safePaddingX = Mathf.Min(settings.BoundsPadding, Mathf.Max(0f, (resolvedMaxBounds.x - resolvedMinBounds.x) * 0.5f));
        float safePaddingY = Mathf.Min(settings.BoundsPadding, Mathf.Max(0f, (resolvedMaxBounds.y - resolvedMinBounds.y) * 0.5f));
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

        int obstacleMask = settings.ObstacleLayerMask;
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
                return colliderRadius + settings.SpawnClearance;
            }
        }

        if (!hasLoggedFallbackCollider)
        {
            string enemyName = enemyDefinition != null ? enemyDefinition.name : "unknown";
            Debug.LogWarning($"[{nameof(SpawnPositionResolver)}] Enemy '{enemyName}' has no supported root Collider2D for spawn occupancy checks. Using fallback radius {FALLBACK_OCCUPANCY_RADIUS:0.###}.");
            hasLoggedFallbackCollider = true;
        }

        return FALLBACK_OCCUPANCY_RADIUS + settings.SpawnClearance;
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

}
