#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class WaveJsonFile
{
    public List<WaveJsonWave> waves = new();
}

[Serializable]
public sealed class WaveJsonWave
{
    public string waveId;
    public string displayName;
    public float durationSeconds;
    public string completionMode;
    public WaveJsonSpawnLocation spawnLocation;
}

[Serializable]
public sealed class WaveJsonSpawnLocation
{
    public WaveJsonSpawnLocationResolverSettings resolverSettings;
    public WaveJsonSpawnLocationStrategy strategy;
}

[Serializable]
public sealed class WaveJsonSpawnLocationResolverSettings
{
    public float boundsPadding;
    public int resolveAttempts;
    public float spawnClearance;
    public WaveJsonVector2 minBounds;
    public WaveJsonVector2 maxBounds;
    public List<string> obstacleLayerNames = new();
}

[Serializable]
public sealed class WaveJsonSpawnLocationStrategy
{
    public string type;
    public float minDistance;
    public float maxDistance;
}

[Serializable]
public sealed class WaveJsonVector2
{
    public float x;
    public float y;
}
#endif
