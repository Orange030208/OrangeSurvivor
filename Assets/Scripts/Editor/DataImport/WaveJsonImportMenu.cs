#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WaveJsonImportMenu
{
    [MenuItem("Survivors/Data Import/Preview Waves From JSON")]
    public static void PreviewWaves()
    {
        IReadOnlyList<WaveJsonWave> waves = WaveJsonReader.ReadDefault();
        DataImportReport report = WaveJsonAssetSync.Preview(waves);
        Debug.Log($"Wave JSON preview: {report.ToSummary()}\n{report.ToMarkdown()}");
    }

    [MenuItem("Survivors/Data Import/Apply Waves From JSON")]
    public static void ApplyWaves()
    {
        IReadOnlyList<WaveJsonWave> waves = WaveJsonReader.ReadDefault();
        DataImportReport preview = WaveJsonAssetSync.Preview(waves);
        if (preview.HasBlockers)
        {
            Debug.LogError($"Wave JSON import blocked: {preview.ToSummary()}\n{preview.ToMarkdown()}");
            return;
        }

        DataImportReport report = WaveJsonAssetSync.Apply(waves);
        AssetDatabase.Refresh();
        Debug.Log($"Wave JSON import applied: {report.ToSummary()}\n{report.ToMarkdown()}");
    }
}
#endif
