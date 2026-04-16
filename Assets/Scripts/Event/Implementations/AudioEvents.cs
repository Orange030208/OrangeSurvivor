public struct AudioPlayRequestedEvent : IGameEvent
{
    public AudioPlaybackRequest Request;

    public AudioPlayRequestedEvent(AudioPlaybackRequest request)
    {
        Request = request;
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
