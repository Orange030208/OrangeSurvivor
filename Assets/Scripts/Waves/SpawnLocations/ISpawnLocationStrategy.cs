using UnityEngine;

public interface ISpawnLocationStrategy
{
    Vector2 CreateCandidatePosition(SpawnLocationStrategyContext context);
}
