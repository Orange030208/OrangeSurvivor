using System;
using UnityEngine;
using Random = UnityEngine.Random;

public readonly struct AudioCueData
{
    public string CueId { get; }
    public AudioClip Clip { get; }
    public AudioSfxClipVariant[] ClipVariants { get; }
    public AudioBusType BusType { get; }
    public AudioPlaybackMode PlaybackMode { get; }
    public float Pitch { get; }
    public float PitchMin { get; }
    public float PitchMax { get; }
    public float VolumeScale { get; }
    public string GroupId { get; }
    public int MaxConcurrent { get; }
    public float Cooldown { get; }
    public int Priority { get; }
    public bool Use2DSpatialBlend { get; }

    public AudioCueData(string cueId, AudioClip clip, AudioBusType busType, AudioPlaybackMode playbackMode, float pitch)
        : this(cueId, clip, busType, playbackMode, pitch, AudioConstants.DEFAULT_VOLUME, AudioConstants.DEFAULT_SFX_GROUP_ID,
            AudioConstants.DEFAULT_CUE_MAX_CONCURRENT, 0f, AudioConstants.DEFAULT_AUDIO_PRIORITY, false)
    {
    }

    public AudioCueData(
        string cueId,
        AudioClip clip,
        AudioBusType busType,
        AudioPlaybackMode playbackMode,
        float pitch,
        float volumeScale,
        string groupId,
        int maxConcurrent,
        float cooldown,
        int priority,
        bool use2DSpatialBlend,
        AudioSfxClipVariant[] clipVariants = null,
        float pitchMin = float.NaN,
        float pitchMax = float.NaN)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            throw new ArgumentException("Audio cue id cannot be null or empty.", nameof(cueId));
        }

        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip), $"Audio cue '{cueId}' clip cannot be null.");
        }

        CueId = cueId;
        Clip = clip;
        ClipVariants = clipVariants ?? System.Array.Empty<AudioSfxClipVariant>();
        BusType = busType;
        PlaybackMode = playbackMode;
        Pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        PitchMin = float.IsNaN(pitchMin) ? Pitch : Mathf.Clamp(pitchMin, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        PitchMax = float.IsNaN(pitchMax) ? Pitch : Mathf.Clamp(pitchMax, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
        if (PitchMin > PitchMax)
        {
            (PitchMin, PitchMax) = (PitchMax, PitchMin);
        }

        VolumeScale = Mathf.Clamp(volumeScale, AudioConstants.MIN_VOLUME, AudioConstants.MAX_VOLUME);
        GroupId = string.IsNullOrWhiteSpace(groupId) ? AudioConstants.DEFAULT_SFX_GROUP_ID : groupId.Trim();
        MaxConcurrent = Mathf.Clamp(maxConcurrent, AudioConstants.MIN_CONCURRENT_COUNT, AudioConstants.MAX_CONCURRENT_COUNT);
        Cooldown = Mathf.Max(0f, cooldown);
        Priority = Mathf.Clamp(priority, 0, 256);
        Use2DSpatialBlend = use2DSpatialBlend;
    }

    public AudioClip SelectClip()
    {
        if (ClipVariants == null || ClipVariants.Length == 0)
        {
            return Clip;
        }

        float totalWeight = 0f;
        for (int i = 0; i < ClipVariants.Length; i++)
        {
            AudioSfxClipVariant variant = ClipVariants[i];
            if (variant != null && variant.IsValid)
            {
                totalWeight += variant.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return Clip;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < ClipVariants.Length; i++)
        {
            AudioSfxClipVariant variant = ClipVariants[i];
            if (variant == null || !variant.IsValid)
            {
                continue;
            }

            roll -= variant.Weight;
            if (roll <= 0f)
            {
                return variant.Clip;
            }
        }

        return Clip;
    }

    public float SelectPitch()
    {
        if (Mathf.Approximately(PitchMin, PitchMax))
        {
            return Pitch;
        }

        return Random.Range(PitchMin, PitchMax);
    }
}
