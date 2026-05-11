using UnityEngine;

public readonly struct ScreenShakeRequest
{
    public ScreenShakeSettings Settings { get; }
    public float StrengthScale { get; }
    public bool HasSourcePosition { get; }
    public Vector2 SourcePosition { get; }

    public ScreenShakeRequest(ScreenShakeSettings settings, float strengthScale = 1f)
    {
        Settings = settings;
        StrengthScale = strengthScale;
        HasSourcePosition = false;
        SourcePosition = default;
    }

    public ScreenShakeRequest(ScreenShakeSettings settings, float strengthScale, Vector2 sourcePosition)
    {
        Settings = settings;
        StrengthScale = strengthScale;
        HasSourcePosition = true;
        SourcePosition = sourcePosition;
    }
}
