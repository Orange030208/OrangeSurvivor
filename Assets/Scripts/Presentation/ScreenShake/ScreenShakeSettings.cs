using System;
using UnityEngine;

[Serializable]
public sealed class ScreenShakeSettings
{
    private const float MIN_DURATION = 0.01f;
    private const float MIN_FREQUENCY = 0.01f;

    [SerializeField] private bool enabled = true;
    [SerializeField, Min(MIN_DURATION)] private float duration = 0.18f;
    [SerializeField, Min(0f)] private float positionStrength = 0.28f;
    [SerializeField, Min(0f)] private float rotationStrength = 1.6f;
    [SerializeField, Min(0f)] private float zoomStrength = 0f;
    [SerializeField, Min(MIN_FREQUENCY)] private float frequency = 38f;
    [SerializeField, Min(0f)] private float strengthScale = 1f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private AnimationCurve fadeCurve = CreateDefaultFadeCurve();

    public bool Enabled => enabled;
    public float Duration => duration;
    public float PositionStrength => positionStrength;
    public float RotationStrength => rotationStrength;
    public float ZoomStrength => zoomStrength;
    public float Frequency => frequency;
    public float StrengthScale => strengthScale;
    public bool UseUnscaledTime => useUnscaledTime;
    public AnimationCurve FadeCurve => fadeCurve ?? CreateDefaultFadeCurve();
    public bool HasAnyStrength => positionStrength > 0f || rotationStrength > 0f || zoomStrength > 0f;
    public bool CanPlay => enabled && duration > 0f && frequency > 0f && strengthScale > 0f && HasAnyStrength;

    public ScreenShakeSettings()
    {
    }

    public ScreenShakeSettings(
        bool enabled,
        float duration,
        float positionStrength,
        float rotationStrength,
        float frequency,
        float zoomStrength = 0f,
        bool useUnscaledTime = true,
        AnimationCurve fadeCurve = null,
        float strengthScale = 1f)
    {
        this.enabled = enabled;
        this.duration = duration;
        this.positionStrength = positionStrength;
        this.rotationStrength = rotationStrength;
        this.frequency = frequency;
        this.zoomStrength = zoomStrength;
        this.useUnscaledTime = useUnscaledTime;
        this.fadeCurve = fadeCurve ?? CreateDefaultFadeCurve();
        this.strengthScale = strengthScale;
    }

    public void OnValidate()
    {
        duration = Mathf.Max(MIN_DURATION, duration);
        positionStrength = Mathf.Max(0f, positionStrength);
        rotationStrength = Mathf.Max(0f, rotationStrength);
        zoomStrength = Mathf.Max(0f, zoomStrength);
        frequency = Mathf.Max(MIN_FREQUENCY, frequency);
        strengthScale = Mathf.Max(0f, strengthScale);

        if (fadeCurve == null || fadeCurve.length == 0)
        {
            fadeCurve = CreateDefaultFadeCurve();
        }
    }

    public static ScreenShakeSettings CreateBossMeleeDefault()
    {
        ScreenShakeSettings settings = new(
            true,
            0.18f,
            0.32f,
            2.2f,
            42f,
            0.08f,
            true);
        settings.fadeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.35f, 0.72f),
            new Keyframe(1f, 0f));
        return settings;
    }

    public static AnimationCurve CreateDefaultFadeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
    }
}
