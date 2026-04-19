using UnityEngine;

public readonly struct SpawnContext
{
    public readonly IEntity AnchorEntity;
    public readonly float ElapsedTime;
    public readonly int WaveIndex;

    public SpawnContext(IEntity anchorEntity, float elapsedTime, int waveIndex)
    {
        AnchorEntity = anchorEntity;
        ElapsedTime = elapsedTime;
        WaveIndex = waveIndex;
    }
}
