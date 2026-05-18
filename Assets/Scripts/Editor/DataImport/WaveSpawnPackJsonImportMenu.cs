#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WaveSpawnPackJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Wave Spawn Packs From JSON")]
    public static void PreviewWaveSpawnPacks()
    {
        IReadOnlyList<WaveSpawnPackJsonPack> spawnPacks = WaveSpawnPackJsonReader.ReadDefault();
        DataImportReport report = WaveSpawnPackJsonAssetSync.Preview(spawnPacks);
        Debug.Log($"Wave spawn pack JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Wave Spawn Packs From JSON")]
    public static void ApplyWaveSpawnPacks()
    {
        IReadOnlyList<WaveSpawnPackJsonPack> spawnPacks = WaveSpawnPackJsonReader.ReadDefault();
        DataImportReport preview = WaveSpawnPackJsonAssetSync.Preview(spawnPacks);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Wave spawn pack JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = WaveSpawnPackJsonAssetSync.Apply(spawnPacks);
        AssetDatabase.Refresh();
        Debug.Log($"Wave spawn pack JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
