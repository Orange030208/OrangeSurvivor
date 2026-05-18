using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class RandomInsideMapSpawnLocationStrategy : SpawnLocationStrategyModel
{
    public override Vector2 CreateCandidatePosition(SpawnLocationStrategyContext context)
    {
        return new Vector2(
            Random.Range(context.MinBounds.x, context.MaxBounds.x),
            Random.Range(context.MinBounds.y, context.MaxBounds.y));
    }
}
