using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class AroundPlayerRingSpawnLocationStrategy : SpawnLocationStrategyModel
{
    private const float MIN_DISTANCE = 0.1f;

    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 10f;

    public float MinDistance => Mathf.Max(MIN_DISTANCE, minDistance);
    public float MaxDistance => Mathf.Max(MinDistance, maxDistance);

    public AroundPlayerRingSpawnLocationStrategy()
    {
    }

    public AroundPlayerRingSpawnLocationStrategy(float minDistance, float maxDistance)
    {
        this.minDistance = Mathf.Max(MIN_DISTANCE, minDistance);
        this.maxDistance = Mathf.Max(this.minDistance, maxDistance);
    }

    public override Vector2 CreateCandidatePosition(SpawnLocationStrategyContext context)
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.up;
        }

        float spawnDistance = Random.Range(MinDistance, MaxDistance);
        return ClampInsideBounds(context.AnchorPosition + direction * spawnDistance, context.MinBounds, context.MaxBounds);
    }

    private static Vector2 ClampInsideBounds(Vector2 position, Vector2 minBounds, Vector2 maxBounds)
    {
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
        return position;
    }
}
