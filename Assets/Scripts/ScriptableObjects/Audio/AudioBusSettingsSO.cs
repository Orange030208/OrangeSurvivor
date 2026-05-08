using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Audio Bus Settings", menuName = ScriptableObjectMenuPaths.AUDIO_BUS_SETTINGS, order = 0)]
public class AudioBusSettingsSO : ScriptableObject
{
    [Header("Volumes")]
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float musicVolume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float sfxVolume = AudioConstants.DEFAULT_VOLUME;

    [Header("SFX Groups")]
    [SerializeField] private string defaultSfxGroupId = AudioConstants.DEFAULT_SFX_GROUP_ID;
    [SerializeField] private AudioSfxGroupSettings[] sfxGroups = CreateDefaultGroups();

    [Header("2D Spatial")]
    [SerializeField] [Min(AudioConstants.MIN_2D_AUDIBLE_DISTANCE)] private float audibleDistance = AudioConstants.DEFAULT_2D_AUDIBLE_DISTANCE;

    private Dictionary<string, AudioSfxGroupSettings> sfxGroupCache;
    private Dictionary<AudioSfxKey, AudioCueData> sfxCueCache;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public string DefaultSfxGroupId => NormalizeGroupId(defaultSfxGroupId);
    public float AudibleDistance => Mathf.Max(AudioConstants.MIN_2D_AUDIBLE_DISTANCE, audibleDistance);

    public bool TryGetSfxGroup(string groupId, out AudioSfxGroupSettings settings)
    {
        EnsureGroupCache();
        return sfxGroupCache.TryGetValue(NormalizeGroupId(groupId), out settings);
    }

    public AudioSfxGroupSettings GetDefaultSfxGroup()
    {
        if (TryGetSfxGroup(DefaultSfxGroupId, out AudioSfxGroupSettings settings))
        {
            return settings;
        }

        return new AudioSfxGroupSettings(AudioConstants.DEFAULT_SFX_GROUP_ID, AudioConstants.DEFAULT_VOLUME,
            AudioConstants.DEFAULT_SFX_POOL_SIZE, AudioConstants.DEFAULT_SFX_MAX_CONCURRENT,
            AudioConstants.DEFAULT_AUDIO_PRIORITY, false);
    }

    public bool TryGetSfxCue(AudioSfxKey sfxKey, out AudioCueData cueData)
    {
        cueData = default;
        if (sfxKey == AudioSfxKey.None)
        {
            return false;
        }

        EnsureSfxCueCache();
        return sfxCueCache.TryGetValue(sfxKey, out cueData);
    }

    public static string NormalizeGroupId(string groupId)
    {
        return string.IsNullOrWhiteSpace(groupId) ? AudioConstants.DEFAULT_SFX_GROUP_ID : groupId.Trim();
    }

    private void OnValidate()
    {
        musicVolume = Mathf.Clamp(musicVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        sfxVolume = Mathf.Clamp(sfxVolume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        defaultSfxGroupId = NormalizeGroupId(defaultSfxGroupId);
        audibleDistance = Mathf.Max(AudioConstants.MIN_2D_AUDIBLE_DISTANCE, audibleDistance);

        if (sfxGroups == null || sfxGroups.Length == 0)
        {
            sfxGroups = CreateDefaultGroups();
        }

        for (int i = 0; i < sfxGroups.Length; i++)
        {
            sfxGroups[i]?.OnValidate();
        }

        sfxGroupCache = null;
        sfxCueCache = null;
    }

    private void EnsureGroupCache()
    {
        if (sfxGroupCache != null)
        {
            return;
        }

        sfxGroupCache = new Dictionary<string, AudioSfxGroupSettings>(StringComparer.Ordinal);
        AudioSfxGroupSettings[] groups = sfxGroups == null || sfxGroups.Length == 0 ? CreateDefaultGroups() : sfxGroups;
        for (int i = 0; i < groups.Length; i++)
        {
            AudioSfxGroupSettings group = groups[i];
            if (group == null)
            {
                continue;
            }

            sfxGroupCache[group.GroupId] = group;
        }

        if (!sfxGroupCache.ContainsKey(AudioConstants.DEFAULT_SFX_GROUP_ID))
        {
            sfxGroupCache[AudioConstants.DEFAULT_SFX_GROUP_ID] = new AudioSfxGroupSettings(
                AudioConstants.DEFAULT_SFX_GROUP_ID,
                AudioConstants.DEFAULT_VOLUME,
                AudioConstants.DEFAULT_SFX_POOL_SIZE,
                AudioConstants.DEFAULT_SFX_MAX_CONCURRENT,
                AudioConstants.DEFAULT_AUDIO_PRIORITY,
                false);
        }
    }

    private void EnsureSfxCueCache()
    {
        if (sfxCueCache != null)
        {
            return;
        }

        EnsureGroupCache();
        sfxCueCache = new Dictionary<AudioSfxKey, AudioCueData>();
        foreach (AudioSfxGroupSettings group in sfxGroupCache.Values)
        {
            if (group == null || group.SfxEntries == null)
            {
                continue;
            }

            for (int i = 0; i < group.SfxEntries.Count; i++)
            {
                AudioSfxEntry entry = group.SfxEntries[i];
                if (entry == null || !entry.TryBuild(group.GroupId, out AudioCueData cueData))
                {
                    continue;
                }

                sfxCueCache[entry.SfxKey] = cueData;
            }
        }
    }

    private static AudioSfxGroupSettings[] CreateDefaultGroups()
    {
        return new[]
        {
            new AudioSfxGroupSettings(AudioConstants.DEFAULT_SFX_GROUP_ID, 1f, 16, 12, 128, false),
            new AudioSfxGroupSettings(AudioConstants.UI_SFX_GROUP_ID, 1f, 12, 10, 80, false),
            new AudioSfxGroupSettings(AudioConstants.COMBAT_SFX_GROUP_ID, 1f, 24, 16, 128, true),
            new AudioSfxGroupSettings(AudioConstants.PICKUP_SFX_GROUP_ID, 1f, 12, 8, 120, true),
            new AudioSfxGroupSettings(AudioConstants.AMBIENT_SFX_GROUP_ID, 1f, 8, 4, 160, true)
        };
    }
}
