using UnityEngine;

public readonly struct HeartSteelDwellSettings
{
    public HeartSteelDwellSettings(float requiredDwellSeconds, float lingerSeconds)
    {
        RequiredDwellSeconds = Mathf.Max(0f, requiredDwellSeconds);
        LingerSeconds = Mathf.Max(0f, lingerSeconds);
    }

    public float RequiredDwellSeconds { get; }
    public float LingerSeconds { get; }
}
