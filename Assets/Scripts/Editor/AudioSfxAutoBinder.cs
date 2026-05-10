using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 按音频文件名自动绑定 AudioBusSettingsSO 中已有的 SFX 引用。
/// 规则很直接：AudioClip.name 与 AudioSfxKey 枚举名完全一致时，自动写入主引用。
/// </summary>
public static class AudioSfxAutoBinder
{
    private const string MENU_PATH = "Survivors/Audio/自动绑定 SFX 音频";
    private const string SETTINGS_PATH = GameContentAssetPaths.AudioBusSettings;

    [MenuItem(MENU_PATH)]
    public static void BindAudioClips()
    {
        AudioBusSettingsSO settings = LoadSettingsAsset();
        if (settings == null)
        {
            Debug.LogError($"{nameof(AudioSfxAutoBinder)} 未找到音频总线配置，期望路径：{SETTINGS_PATH}");
            return;
        }

        Dictionary<string, AudioClip> clipsByName = BuildClipMap(out int scannedClipCount, out int duplicateNameCount);
        if (clipsByName.Count == 0)
        {
            Debug.LogWarning($"{nameof(AudioSfxAutoBinder)} 未扫描到任何 AudioClip。");
            return;
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        serializedSettings.Update();

        SerializedProperty groupsProperty = serializedSettings.FindProperty("sfxGroups");
        if (groupsProperty == null)
        {
            Debug.LogError($"{nameof(AudioSfxAutoBinder)} 无法找到 AudioBusSettingsSO.sfxGroups 序列化字段。");
            return;
        }

        int matchedEntryCount = 0;
        int changedEntryCount = 0;
        int missingClipCount = 0;
        int invalidKeyCount = 0;
        HashSet<string> missingKeyNames = new HashSet<string>(StringComparer.Ordinal);

        for (int groupIndex = 0; groupIndex < groupsProperty.arraySize; groupIndex++)
        {
            SerializedProperty groupProperty = groupsProperty.GetArrayElementAtIndex(groupIndex);
            SerializedProperty entriesProperty = groupProperty.FindPropertyRelative("sfxEntries");
            if (entriesProperty == null)
            {
                continue;
            }

            for (int entryIndex = 0; entryIndex < entriesProperty.arraySize; entryIndex++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(entryIndex);
                SerializedProperty keyProperty = entryProperty.FindPropertyRelative("sfxKey");
                SerializedProperty clipProperty = entryProperty.FindPropertyRelative("clip");

                if (keyProperty == null || clipProperty == null)
                {
                    continue;
                }

                int keyValue = keyProperty.intValue;
                if (keyValue == (int)AudioSfxKey.None)
                {
                    continue;
                }

                string keyName = Enum.GetName(typeof(AudioSfxKey), keyValue);
                if (string.IsNullOrWhiteSpace(keyName))
                {
                    invalidKeyCount++;
                    continue;
                }

                if (!clipsByName.TryGetValue(keyName, out AudioClip clip) || clip == null)
                {
                    missingClipCount++;
                    missingKeyNames.Add(keyName);
                    continue;
                }

                matchedEntryCount++;
                if (clipProperty.objectReferenceValue == clip)
                {
                    continue;
                }

                clipProperty.objectReferenceValue = clip;
                changedEntryCount++;
            }
        }

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"{nameof(AudioSfxAutoBinder)} 完成自动绑定。扫描音频 {scannedClipCount} 个，命中条目 {matchedEntryCount} 个，" +
            $"实际改动 {changedEntryCount} 个，缺失音频 {missingClipCount} 个，重复命名 {duplicateNameCount} 个。");

        if (invalidKeyCount > 0)
        {
            Debug.LogWarning($"{nameof(AudioSfxAutoBinder)} 发现 {invalidKeyCount} 个无法解析的音效枚举值。");
        }

        if (missingKeyNames.Count > 0)
        {
            List<string> sortedMissingKeys = new List<string>(missingKeyNames);
            sortedMissingKeys.Sort(StringComparer.Ordinal);
            Debug.LogWarning(
                $"{nameof(AudioSfxAutoBinder)} 下列音效未找到同名音频：{string.Join(", ", sortedMissingKeys)}");
        }
    }

    private static AudioBusSettingsSO LoadSettingsAsset()
    {
        AudioBusSettingsSO settings = AssetDatabase.LoadAssetAtPath<AudioBusSettingsSO>(SETTINGS_PATH);
        if (settings != null)
        {
            return settings;
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioBusSettingsSO");
        AudioBusSettingsSO fallbackSettings = null;
        string fallbackPath = null;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioBusSettingsSO candidate = AssetDatabase.LoadAssetAtPath<AudioBusSettingsSO>(assetPath);
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(assetPath, SETTINGS_PATH, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            if (fallbackSettings == null)
            {
                fallbackSettings = candidate;
                fallbackPath = assetPath;
            }
        }

        if (fallbackSettings != null)
        {
            Debug.LogWarning(
                $"{nameof(AudioSfxAutoBinder)} 未找到预期路径 {SETTINGS_PATH}，已改用 {fallbackPath}。");
        }

        return fallbackSettings;
    }

    private static Dictionary<string, AudioClip> BuildClipMap(out int scannedClipCount, out int duplicateNameCount)
    {
        Dictionary<string, ClipCandidate> bestCandidatesByName = new Dictionary<string, ClipCandidate>(StringComparer.Ordinal);
        string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip");

        scannedClipCount = 0;
        duplicateNameCount = 0;

        for (int i = 0; i < clipGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                continue;
            }

            scannedClipCount++;
            string clipName = clip.name;
            if (string.IsNullOrWhiteSpace(clipName))
            {
                continue;
            }

            ClipCandidate candidate = new ClipCandidate(clip, assetPath, GetClipScore(assetPath));
            if (!bestCandidatesByName.TryGetValue(clipName, out ClipCandidate existingCandidate))
            {
                bestCandidatesByName.Add(clipName, candidate);
                continue;
            }

            duplicateNameCount++;
            if (candidate.IsBetterThan(existingCandidate))
            {
                Debug.LogWarning(
                    $"{nameof(AudioSfxAutoBinder)} 发现同名音频 {clipName}，选择 {assetPath}，忽略 {existingCandidate.AssetPath}。");
                bestCandidatesByName[clipName] = candidate;
            }
            else
            {
                Debug.LogWarning(
                    $"{nameof(AudioSfxAutoBinder)} 发现同名音频 {clipName}，保留 {existingCandidate.AssetPath}，忽略 {assetPath}。");
            }
        }

        Dictionary<string, AudioClip> clipMap = new Dictionary<string, AudioClip>(bestCandidatesByName.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, ClipCandidate> pair in bestCandidatesByName)
        {
            clipMap.Add(pair.Key, pair.Value.Clip);
        }

        return clipMap;
    }

    private static int GetClipScore(string assetPath)
    {
        if (assetPath.StartsWith(GameContentAssetPaths.AudioSfx, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (assetPath.IndexOf("/Audio/SFX/", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 1;
        }

        return 0;
    }

    private sealed class ClipCandidate
    {
        public readonly AudioClip Clip;
        public readonly string AssetPath;
        public readonly int Score;

        public ClipCandidate(AudioClip clip, string assetPath, int score)
        {
            Clip = clip;
            AssetPath = assetPath;
            Score = score;
        }

        public bool IsBetterThan(ClipCandidate other)
        {
            if (Score != other.Score)
            {
                return Score > other.Score;
            }

            return string.Compare(AssetPath, other.AssetPath, StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
