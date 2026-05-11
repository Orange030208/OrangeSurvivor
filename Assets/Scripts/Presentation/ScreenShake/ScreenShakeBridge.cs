using UnityEngine;

public static class ScreenShakeBridge
{
    public static void Request(ScreenShakeSettings settings, float strengthScale = 1f)
    {
        Request(new ScreenShakeRequest(settings, strengthScale));
    }

    public static void Request(ScreenShakeSettings settings, float strengthScale, Vector2 sourcePosition)
    {
        Request(new ScreenShakeRequest(settings, strengthScale, sourcePosition));
    }

    public static void Request(ScreenShakeSettings settings, float strengthScale, Vector3 sourcePosition)
    {
        Request(settings, strengthScale, (Vector2)sourcePosition);
    }

    public static void Request(ScreenShakeRequest request)
    {
        if (!CanRequest(request))
        {
            return;
        }

        GameEventBus.Publish(new ScreenShakeRequestedEvent(request));
    }

    public static bool CanRequest(ScreenShakeRequest request)
    {
        return CanRequest(request.Settings, request.StrengthScale);
    }

    public static bool CanRequest(ScreenShakeSettings settings, float strengthScale)
    {
        return settings != null && settings.CanPlay && strengthScale > 0f;
    }
}
