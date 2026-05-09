using System;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public struct WaveSegment
{
    [SerializeField] private WaveSpawnIdentity spawnIdentity;

    [MinMaxSlider(0, 100)]
    [SerializeField] private Vector2 timeStartEnd;

    public WaveSpawnIdentity SpawnIdentity => spawnIdentity;
    public string TrackId => spawnIdentity.TrackId;
    public WaveSpawnTriggerMode TriggerMode => spawnIdentity.TriggerMode;
    public float SpawnFrequency => spawnIdentity.SpawnFrequency;
    public int SpawnCountPerBatch => spawnIdentity.SpawnCountPerBatch;
    public int MaxSpawnBatches => spawnIdentity.MaxSpawnBatches;
    public Vector2 TimeStartEnd => timeStartEnd;

    public WaveSegment(WaveSpawnIdentity spawnIdentity, Vector2 timeStartEnd)
    {
        this.spawnIdentity = spawnIdentity;
        this.timeStartEnd = timeStartEnd;
    }
}
