using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioSfxGroupSettings
{
    [SerializeField] private string groupId = AudioConstants.DEFAULT_SFX_GROUP_ID;
    [SerializeField] [Range(AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME)] private float volume = AudioConstants.DEFAULT_VOLUME;
    [SerializeField] [Range(AudioConstants.MIN_POOL_SIZE, AudioConstants.MAX_POOL_SIZE)] private int poolSize = AudioConstants.DEFAULT_SFX_POOL_SIZE;
    [SerializeField] [Range(AudioConstants.MIN_CONCURRENT_COUNT, AudioConstants.MAX_CONCURRENT_COUNT)] private int maxConcurrent = AudioConstants.DEFAULT_SFX_MAX_CONCURRENT;
    [SerializeField] [Range(0, 256)] private int priority = AudioConstants.DEFAULT_AUDIO_PRIORITY;
    [SerializeField] private bool spatializeByDefault;
    [SerializeField] private AudioSfxEntry[] sfxEntries = Array.Empty<AudioSfxEntry>();

    public string GroupId => string.IsNullOrWhiteSpace(groupId) ? AudioConstants.DEFAULT_SFX_GROUP_ID : groupId.Trim();
    public float Volume => volume;
    public int PoolSize => poolSize;
    public int MaxConcurrent => maxConcurrent;
    public int Priority => priority;
    public bool SpatializeByDefault => spatializeByDefault;
    public IReadOnlyList<AudioSfxEntry> SfxEntries => sfxEntries;

    public AudioSfxGroupSettings()
    {
    }

    public AudioSfxGroupSettings(string groupId, float volume, int poolSize, int maxConcurrent, int priority, bool spatializeByDefault)
    {
        this.groupId = groupId;
        this.volume = volume;
        this.poolSize = poolSize;
        this.maxConcurrent = maxConcurrent;
        this.priority = priority;
        this.spatializeByDefault = spatializeByDefault;
        OnValidate();
    }

    public void OnValidate()
    {
        groupId = string.IsNullOrWhiteSpace(groupId) ? AudioConstants.DEFAULT_SFX_GROUP_ID : groupId.Trim();
        volume = Mathf.Clamp(volume, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        poolSize = Mathf.Clamp(poolSize, AudioConstants.MIN_POOL_SIZE, AudioConstants.MAX_POOL_SIZE);
        maxConcurrent = Mathf.Clamp(maxConcurrent, AudioConstants.MIN_CONCURRENT_COUNT, AudioConstants.MAX_CONCURRENT_COUNT);
        priority = Mathf.Clamp(priority, 0, 256);

        if (sfxEntries == null)
        {
            sfxEntries = Array.Empty<AudioSfxEntry>();
        }

        for (int i = 0; i < sfxEntries.Length; i++)
        {
            sfxEntries[i]?.OnValidate();
        }
    }
}
