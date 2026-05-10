using System;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class DamageTextVisualStyle
{
    private const string DEFAULT_NUMBER_FORMAT = "0";
    private const float MIN_DURATION = 0.01f;

    [Header("文本")]
    [SerializeField] private string prefix = string.Empty;
    [SerializeField] private string suffix = string.Empty;
    [SerializeField] private string numberFormat = DEFAULT_NUMBER_FORMAT;
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] [Min(0.01f)] private float fontSize = 4.3f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool useVertexGradient = true;
    [SerializeField] private Color gradientTopColor = Color.white;
    [SerializeField] private Color gradientBottomColor = new(0.85f, 0.92f, 1f, 1f);

    [Header("运动")]
    [SerializeField] [Min(MIN_DURATION)] private float lifetime = 0.85f;
    [SerializeField] [Min(0f)] private float floatDistance = 0.85f;
    [SerializeField] [Min(0f)] private float horizontalDrift = 0.18f;
    [SerializeField] [Min(0f)] private float startScale = 0.65f;
    [SerializeField] [Min(0f)] private float peakScale = 1.18f;
    [SerializeField] [Min(0f)] private float endScale = 1f;
    [SerializeField] [Min(MIN_DURATION)] private float popDuration = 0.14f;
    [SerializeField] [Min(MIN_DURATION)] private float settleDuration = 0.12f;
    [SerializeField] private Ease popEase = Ease.OutBack;
    [SerializeField] private Ease settleEase = Ease.OutQuad;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("淡出")]
    [SerializeField] private bool useFade = true;
    [SerializeField] [Min(0f)] private float fadeDelay = 0.34f;
    [SerializeField] private Ease fadeEase = Ease.InQuad;

    [Header("冲击反馈")]
    [SerializeField] [Min(0f)] private float shakeStrength = 0f;
    [SerializeField] [Min(MIN_DURATION)] private float shakeDuration = 0.16f;
    [SerializeField] [Range(1, 40)] private int shakeVibrato = 14;
    [SerializeField] [Range(0f, 180f)] private float shakeRandomness = 75f;

    public string Prefix => prefix ?? string.Empty;
    public string Suffix => suffix ?? string.Empty;
    public string NumberFormat => string.IsNullOrWhiteSpace(numberFormat) ? DEFAULT_NUMBER_FORMAT : numberFormat;
    public FontStyles FontStyle => fontStyle;
    public float FontSize => Mathf.Max(0.01f, fontSize);
    public Color TextColor => textColor;
    public bool UseVertexGradient => useVertexGradient;
    public Color GradientTopColor => gradientTopColor;
    public Color GradientBottomColor => gradientBottomColor;
    public float Lifetime => Mathf.Max(MIN_DURATION, lifetime);
    public float FloatDistance => Mathf.Max(0f, floatDistance);
    public float HorizontalDrift => Mathf.Max(0f, horizontalDrift);
    public float StartScale => Mathf.Max(0f, startScale);
    public float PeakScale => Mathf.Max(0f, peakScale);
    public float EndScale => Mathf.Max(0f, endScale);
    public float PopDuration => Mathf.Max(MIN_DURATION, popDuration);
    public float SettleDuration => Mathf.Max(MIN_DURATION, settleDuration);
    public Ease PopEase => popEase;
    public Ease SettleEase => settleEase;
    public Ease MoveEase => moveEase;
    public bool UseFade => useFade;
    public float FadeDelay => Mathf.Clamp(fadeDelay, 0f, Lifetime);
    public Ease FadeEase => fadeEase;
    public float ShakeStrength => Mathf.Max(0f, shakeStrength);
    public float ShakeDuration => Mathf.Max(MIN_DURATION, shakeDuration);
    public int ShakeVibrato => Mathf.Max(1, shakeVibrato);
    public float ShakeRandomness => Mathf.Clamp(shakeRandomness, 0f, 180f);

    public string FormatDamage(float damage)
    {
        float displayDamage = damage > 0f && damage < 1f ? 1f : damage;
        return Prefix + displayDamage.ToString(NumberFormat, CultureInfo.InvariantCulture) + Suffix;
    }

    public void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(numberFormat))
        {
            numberFormat = DEFAULT_NUMBER_FORMAT;
        }

        fontSize = Mathf.Max(0.01f, fontSize);
        lifetime = Mathf.Max(MIN_DURATION, lifetime);
        floatDistance = Mathf.Max(0f, floatDistance);
        horizontalDrift = Mathf.Max(0f, horizontalDrift);
        startScale = Mathf.Max(0f, startScale);
        peakScale = Mathf.Max(0f, peakScale);
        endScale = Mathf.Max(0f, endScale);
        popDuration = Mathf.Max(MIN_DURATION, popDuration);
        settleDuration = Mathf.Max(MIN_DURATION, settleDuration);
        fadeDelay = Mathf.Clamp(fadeDelay, 0f, lifetime);
        shakeStrength = Mathf.Max(0f, shakeStrength);
        shakeDuration = Mathf.Max(MIN_DURATION, shakeDuration);
        shakeVibrato = Mathf.Max(1, shakeVibrato);
        shakeRandomness = Mathf.Clamp(shakeRandomness, 0f, 180f);
    }

    public static DamageTextVisualStyle CreateDefaultNormal()
    {
        return new DamageTextVisualStyle
        {
            textColor = new Color(0.96f, 0.98f, 1f, 1f),
            gradientTopColor = Color.white,
            gradientBottomColor = new Color(0.72f, 0.9f, 1f, 1f),
            fontSize = 4.3f,
            lifetime = 0.82f,
            floatDistance = 0.78f,
            horizontalDrift = 0.16f,
            startScale = 0.58f,
            peakScale = 1.12f,
            endScale = 0.95f,
            popDuration = 0.13f,
            settleDuration = 0.12f,
            fadeDelay = 0.34f
        };
    }

    public static DamageTextVisualStyle CreateDefaultCritical()
    {
        return new DamageTextVisualStyle
        {
            fontStyle = FontStyles.Bold,
            textColor = new Color(1f, 0.72f, 0.18f, 1f),
            gradientTopColor = new Color(1f, 0.98f, 0.46f, 1f),
            gradientBottomColor = new Color(1f, 0.28f, 0.08f, 1f),
            fontSize = 5.2f,
            lifetime = 0.96f,
            floatDistance = 1.05f,
            horizontalDrift = 0.24f,
            startScale = 0.78f,
            peakScale = 1.42f,
            endScale = 1.08f,
            popDuration = 0.16f,
            settleDuration = 0.16f,
            popEase = Ease.OutElastic,
            fadeDelay = 0.42f,
            shakeStrength = 0.18f,
            shakeDuration = 0.18f,
            shakeVibrato = 18,
            shakeRandomness = 90f
        };
    }

    public static DamageTextVisualStyle CreateDefaultPlayerDamaged()
    {
        return new DamageTextVisualStyle
        {
            fontStyle = FontStyles.Bold,
            textColor = new Color(1f, 0.24f, 0.22f, 1f),
            gradientTopColor = new Color(1f, 0.58f, 0.5f, 1f),
            gradientBottomColor = new Color(0.76f, 0.04f, 0.03f, 1f),
            fontSize = 4.8f,
            lifetime = 0.9f,
            floatDistance = 0.9f,
            horizontalDrift = 0.2f,
            startScale = 0.72f,
            peakScale = 1.28f,
            endScale = 1f,
            popDuration = 0.14f,
            settleDuration = 0.13f,
            fadeDelay = 0.36f,
            shakeStrength = 0.1f,
            shakeDuration = 0.16f,
            shakeVibrato = 14,
            shakeRandomness = 80f
        };
    }

    public static DamageTextVisualStyle CreateLegacyNormal(float lifetime, Color color, float startScale, bool useFade)
    {
        DamageTextVisualStyle style = CreateDefaultNormal();
        style.lifetime = Mathf.Max(MIN_DURATION, lifetime);
        style.textColor = color;
        style.gradientTopColor = color;
        style.gradientBottomColor = color;
        style.useVertexGradient = false;
        style.startScale = Mathf.Max(0f, startScale);
        style.peakScale = 1f;
        style.endScale = 1f;
        style.useFade = useFade;
        style.fadeDelay = Mathf.Clamp(lifetime * 0.5f, 0f, style.lifetime);
        return style;
    }

    public static DamageTextVisualStyle CreateLegacyCritical(
        float lifetime,
        Color color,
        float startScale,
        bool useFade,
        float shakeStrength)
    {
        DamageTextVisualStyle style = CreateDefaultCritical();
        style.lifetime = Mathf.Max(MIN_DURATION, lifetime);
        style.textColor = color;
        style.gradientTopColor = color;
        style.gradientBottomColor = color;
        style.useVertexGradient = false;
        style.startScale = Mathf.Max(0f, startScale);
        style.peakScale = Mathf.Max(1f, startScale);
        style.endScale = 1f;
        style.useFade = useFade;
        style.fadeDelay = Mathf.Clamp(lifetime * 0.4f, 0f, style.lifetime);
        style.shakeStrength = Mathf.Max(0f, shakeStrength);
        return style;
    }
}
