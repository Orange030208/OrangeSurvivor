using System;
using UnityEngine;

[Serializable]
public struct Wave
{
    [SerializeField] private string waveId;
    [SerializeField] private string name;
    [SerializeField] private float duration;
    [SerializeField] private WaveCompletionMode completionMode;
    [SerializeField] private SpawnLocationDefinition spawnLocation;
    [SerializeField] private WaveSegment[] segments;

    public string WaveId => waveId;
    public string Name => name;
    public float Duration => duration;
    public WaveCompletionMode CompletionMode => completionMode;
    public SpawnLocationDefinition SpawnLocation => spawnLocation;
    public WaveSegment[] Segments => segments;

    public Wave(
        string waveId,
        string name,
        float duration,
        WaveCompletionMode completionMode,
        SpawnLocationDefinition spawnLocation,
        WaveSegment[] segments)
    {
        this.waveId = waveId;
        this.name = name;
        this.duration = duration;
        this.completionMode = completionMode;
        this.spawnLocation = spawnLocation ?? SpawnLocationDefinition.CreateDefault();
        this.segments = segments;
    }
}
