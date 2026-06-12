public struct AudioBgmPlayRequestedEvent
{
    public AudioBgmKey BgmKey;
    public bool RestartIfPlaying;

    public AudioBgmPlayRequestedEvent(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        BgmKey = bgmKey;
        RestartIfPlaying = restartIfPlaying;
    }
}

public struct AudioMusicPlayRequestedEvent
{
    public AudioBgmKey BgmKey;
    public bool RestartIfPlaying;

    public AudioMusicPlayRequestedEvent(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        BgmKey = bgmKey;
        RestartIfPlaying = restartIfPlaying;
    }
}

public struct AudioStopRequestedEvent
{
    public AudioBusType BusType;

    public AudioStopRequestedEvent(AudioBusType busType)
    {
        BusType = busType;
    }
}

public enum AudioCommand
{
    MusicStopRequested
}

public struct AudioSfxGroupStopRequestedEvent
{
    public string GroupId;

    public AudioSfxGroupStopRequestedEvent(string groupId)
    {
        GroupId = groupId;
    }
}

public struct AudioSfxGroupVolumeChangedEvent
{
    public string GroupId;
    public float Volume;

    public AudioSfxGroupVolumeChangedEvent(string groupId, float volume)
    {
        GroupId = groupId;
        Volume = volume;
    }
}

public struct AudioSfxPlayRequestedEvent
{
    public AudioSfxKey SfxKey;
    public bool HasPosition;
    public UnityEngine.Vector2 Position;

    public AudioSfxPlayRequestedEvent(AudioSfxKey sfxKey)
    {
        SfxKey = sfxKey;
        HasPosition = false;
        Position = default;
    }

    public AudioSfxPlayRequestedEvent(AudioSfxKey sfxKey, UnityEngine.Vector2 position)
    {
        SfxKey = sfxKey;
        HasPosition = true;
        Position = position;
    }
}
