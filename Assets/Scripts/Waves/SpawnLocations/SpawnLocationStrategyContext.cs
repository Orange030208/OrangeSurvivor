using UnityEngine;

public readonly struct SpawnLocationStrategyContext
{
    public Vector2 AnchorPosition { get; }
    public Vector2 MinBounds { get; }
    public Vector2 MaxBounds { get; }

    public SpawnLocationStrategyContext(Vector2 anchorPosition, Vector2 minBounds, Vector2 maxBounds)
    {
        AnchorPosition = anchorPosition;
        MinBounds = minBounds;
        MaxBounds = maxBounds;
    }
}
