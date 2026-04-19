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

public struct AudioStopRequestedEvent : IGameEvent
{
    public AudioBusType BusType;

    public AudioStopRequestedEvent(AudioBusType busType)
    {
        BusType = busType;
    }
}

public struct AudioSfxPlayRequestedEvent : IGameEvent
{
    public AudioSfxKey SfxKey;

    public AudioSfxPlayRequestedEvent(AudioSfxKey sfxKey)
    {
        SfxKey = sfxKey;
    }
}
