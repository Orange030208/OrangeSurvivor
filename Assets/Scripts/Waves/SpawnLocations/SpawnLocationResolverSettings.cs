using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpawnLocationResolverSettings
{
    private const float MIN_BOUNDS_PADDING = 0f;
    private const int MIN_RESOLVE_ATTEMPTS = 1;
    private const float MIN_SPAWN_CLEARANCE = 0f;
    private const string DEFAULT_OBSTACLE_LAYER_NAME = "Wall";

    private static readonly string[] DefaultObstacleLayerNames = { DEFAULT_OBSTACLE_LAYER_NAME };

    [SerializeField] private float boundsPadding = 1f;
    [SerializeField] private int resolveAttempts = 16;
    [SerializeField] private float spawnClearance = 0.1f;
    [SerializeField] private Vector2 minBounds = new(-12f, -12f);
    [SerializeField] private Vector2 maxBounds = new(12f, 12f);
    [SerializeField] private string[] obstacleLayerNames = DefaultObstacleLayerNames;

    public SpawnLocationResolverSettings()
    {
    }

    public SpawnLocationResolverSettings(
        float boundsPadding,
        int resolveAttempts,
        float spawnClearance,
        Vector2 minBounds,
        Vector2 maxBounds,
        string[] obstacleLayerNames)
    {
        this.boundsPadding = boundsPadding;
        this.resolveAttempts = resolveAttempts;
        this.spawnClearance = spawnClearance;
        this.minBounds = minBounds;
        this.maxBounds = maxBounds;
        this.obstacleLayerNames = obstacleLayerNames;
        Validate();
    }

    public float BoundsPadding => Mathf.Max(MIN_BOUNDS_PADDING, boundsPadding);
    public int ResolveAttempts => Mathf.Max(MIN_RESOLVE_ATTEMPTS, resolveAttempts);
    public float SpawnClearance => Mathf.Max(MIN_SPAWN_CLEARANCE, spawnClearance);
    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;
    public IReadOnlyList<string> ObstacleLayerNames => ResolveObstacleLayerNames();
    public int ObstacleLayerMask => LayerMask.GetMask(ResolveObstacleLayerNames());

    public static SpawnLocationResolverSettings CreateDefault()
    {
        return new SpawnLocationResolverSettings();
    }

    public void Validate()
    {
        boundsPadding = Mathf.Max(MIN_BOUNDS_PADDING, boundsPadding);
        resolveAttempts = Mathf.Max(MIN_RESOLVE_ATTEMPTS, resolveAttempts);
        spawnClearance = Mathf.Max(MIN_SPAWN_CLEARANCE, spawnClearance);
        obstacleLayerNames = ResolveObstacleLayerNames();

        if (maxBounds.x < minBounds.x)
        {
            maxBounds.x = minBounds.x;
        }

        if (maxBounds.y < minBounds.y)
        {
            maxBounds.y = minBounds.y;
        }
    }

    private string[] ResolveObstacleLayerNames()
    {
        if (obstacleLayerNames == null || obstacleLayerNames.Length == 0)
        {
            return DefaultObstacleLayerNames;
        }

        return obstacleLayerNames;
    }
}
