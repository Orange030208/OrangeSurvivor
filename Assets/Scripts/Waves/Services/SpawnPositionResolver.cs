using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPositionResolver
{
    private readonly SpawnLocationPolicyType policyType;
    private readonly float minDistance;
    private readonly float maxDistance;
    private readonly float boundsPadding;
    private readonly int resolveAttempts;
    private readonly Vector2 minBounds;
    private readonly Vector2 maxBounds;

    private SpawnPositionResolver(
        SpawnLocationPolicyType policyType,
        float minDistance,
        float maxDistance,
        float boundsPadding,
        int resolveAttempts,
        Vector2 minBounds,
        Vector2 maxBounds)
    {
        this.policyType = policyType;
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
        this.boundsPadding = boundsPadding;
        this.resolveAttempts = resolveAttempts;
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
            policy.MinBounds,
            policy.MaxBounds);
    }

    public Vector3 Resolve(SpawnContext context)
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

        return policyType switch
        {
            SpawnLocationPolicyType.RandomInsideMap => CreateRandomInsideMapPosition(resolvedMinBounds, resolvedMaxBounds),
            SpawnLocationPolicyType.RandomMapEdge => CreateRandomMapEdgePosition(resolvedMinBounds, resolvedMaxBounds),
            _ => ResolveAroundPlayerRing(context.AnchorEntity.Center, resolvedMinBounds, resolvedMaxBounds)
        };
    }

    private Vector2 ResolveAroundPlayerRing(Vector2 anchorPosition, Vector2 resolvedMinBounds, Vector2 resolvedMaxBounds)
    {
        for (int i = 0; i < resolveAttempts; i++)
        {
            Vector2 targetPosition = CreateRingPosition(anchorPosition);
            if (IsInsideBounds(targetPosition, resolvedMinBounds, resolvedMaxBounds))
            {
                return targetPosition;
            }
        }

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
