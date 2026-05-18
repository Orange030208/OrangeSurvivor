#if UNITY_EDITOR
using System;
using System.Collections.Generic;

[Serializable]
public sealed class WaveSpawnPackJsonFile
{
    public List<WaveSpawnPackJsonPack> spawnPacks = new();
}

[Serializable]
public sealed class WaveSpawnPackJsonPack
{
    public string packId;
    public List<WaveSpawnPackJsonEntry> entries = new();
}

[Serializable]
public sealed class WaveSpawnPackJsonEntry
{
    public string enemyAssetPath;
    public int spawnCount;
    public bool overrideTags = true;
    public List<string> enemyTags = new();
}
#endif
