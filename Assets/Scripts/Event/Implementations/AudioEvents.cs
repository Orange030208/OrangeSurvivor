public struct AudioBgmPlayRequestedEvent : IGameEvent
{
    public AudioBgmKey BgmKey;
    public bool RestartIfPlaying;

    public AudioBgmPlayRequestedEvent(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        BgmKey = bgmKey;
        RestartIfPlaying = restartIfPlaying;
    }
}

public struct AudioMusicPlayRequestedEvent : IGameEvent
{
    public AudioBgmKey BgmKey;
    public bool RestartIfPlaying;

    public AudioMusicPlayRequestedEvent(AudioBgmKey bgmKey, bool restartIfPlaying)
    {
        BgmKey = bgmKey;
        RestartIfPlaying = restartIfPlaying;
    }
}

public struct AudioStopRequestedEvent : IGameEvent
{
    public AudioBusType BusType;

    public AudioStopRequestedEvent(AudioBusType busType)
    {
        BusType = busType;
    }
}

public struct AudioMusicStopRequestedEvent : IGameEvent
{
}

public struct AudioSfxGroupStopRequestedEvent : IGameEvent
{
    public string GroupId;

    public AudioSfxGroupStopRequestedEvent(string groupId)
    {
        GroupId = groupId;
    }
}

public struct AudioSfxGroupVolumeChangedEvent : IGameEvent
{
    public string GroupId;
    public float Volume;

    public AudioSfxGroupVolumeChangedEvent(string groupId, float volume)
    {
        GroupId = groupId;
        Volume = volume;
    }
}

public struct AudioSfxPlayRequestedEvent : IGameEvent
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
