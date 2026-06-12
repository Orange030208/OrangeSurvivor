public struct ScreenShakeRequestedEvent
{
    public ScreenShakeRequest Request;

    public ScreenShakeRequestedEvent(ScreenShakeRequest request)
    {
        Request = request;
    }
}
