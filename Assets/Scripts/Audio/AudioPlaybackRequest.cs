using System;

public readonly struct AudioPlaybackRequest
{
    public string CueId { get; }
    public bool RestartIfPlaying { get; }

    public AudioPlaybackRequest(string cueId, bool restartIfPlaying)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            throw new ArgumentException("Audio playback request cue id cannot be null or empty.", nameof(cueId));
        }

        CueId = cueId;
        RestartIfPlaying = restartIfPlaying;
    }
}
