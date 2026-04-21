using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnPositionResolver
{
    private readonly float minDistance;
    private readonly float maxDistance;
    private readonly Vector2 minBounds;
    private readonly Vector2 maxBounds;

    private SpawnPositionResolver(float minDistance, float maxDistance, Vector2 minBounds, Vector2 maxBounds)
    {
        this.minDistance = minDistance;
        this.maxDistance = maxDistance;
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
    }

    public static SpawnPositionResolver FromPolicy(SpawnLocationPolicySO policy)
    {
        if (policy == null)
        {
            throw new MissingReferenceException($"{nameof(SpawnLocationPolicySO)} is required for wave spawning.");
        }

        return new SpawnPositionResolver(policy.MinDistance, policy.MaxDistance, policy.MinBounds, policy.MaxBounds);
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

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.up;
        }

        float spawnDistance = Random.Range(minDistance, maxDistance);
        Vector2 offset = direction * spawnDistance;
        Vector2 targetPos = (Vector2)context.AnchorEntity.Center + offset;
        targetPos.x = Mathf.Clamp(targetPos.x, resolvedMinBounds.x, resolvedMaxBounds.x);
        targetPos.y = Mathf.Clamp(targetPos.y, resolvedMinBounds.y, resolvedMaxBounds.y);
        return targetPos;
    }
}
