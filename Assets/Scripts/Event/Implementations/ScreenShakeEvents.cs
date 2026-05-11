public struct ScreenShakeRequestedEvent : IGameEvent
{
    public ScreenShakeRequest Request;

    public ScreenShakeRequestedEvent(ScreenShakeRequest request)
    {
        Request = request;
    }
}
