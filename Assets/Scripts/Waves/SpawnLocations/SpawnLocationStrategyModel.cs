using System;
using UnityEngine;

[Serializable]
public abstract class SpawnLocationStrategyModel : ISpawnLocationStrategy
{
    public abstract Vector2 CreateCandidatePosition(SpawnLocationStrategyContext context);
}
