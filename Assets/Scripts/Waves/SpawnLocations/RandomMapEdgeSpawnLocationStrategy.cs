using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class RandomMapEdgeSpawnLocationStrategy : SpawnLocationStrategyModel
{
    public override Vector2 CreateCandidatePosition(SpawnLocationStrategyContext context)
    {
        int edgeIndex = Random.Range(0, 4);
        return edgeIndex switch
        {
            0 => new Vector2(Random.Range(context.MinBounds.x, context.MaxBounds.x), context.MaxBounds.y),
            1 => new Vector2(Random.Range(context.MinBounds.x, context.MaxBounds.x), context.MinBounds.y),
            2 => new Vector2(context.MinBounds.x, Random.Range(context.MinBounds.y, context.MaxBounds.y)),
            _ => new Vector2(context.MaxBounds.x, Random.Range(context.MinBounds.y, context.MaxBounds.y))
        };
    }
}
