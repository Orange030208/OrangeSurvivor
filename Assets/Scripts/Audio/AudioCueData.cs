using System;
using UnityEngine;

public readonly struct AudioCueData
{
    public string CueId { get; }
    public AudioClip Clip { get; }
    public AudioBusType BusType { get; }
    public AudioPlaybackMode PlaybackMode { get; }
    public float Pitch { get; }

    public AudioCueData(string cueId, AudioClip clip, AudioBusType busType, AudioPlaybackMode playbackMode, float pitch)
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
        BusType = busType;
        PlaybackMode = playbackMode;
        Pitch = Mathf.Clamp(pitch, AudioConstants.MIN_PITCH, AudioConstants.MAX_PITCH);
    }
}
