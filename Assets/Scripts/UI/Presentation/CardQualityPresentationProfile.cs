using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct CardQualityPresentationProfile
{
    [SerializeField] private CardQuality quality;
    [SerializeField] private string presentationKey;
    [SerializeField] private AudioSfxKey revealSfxKey;
    [SerializeField] private AudioSfxKey selectSfxKey;
    [SerializeField] private Color backgroundColor;
    [SerializeField] private Color borderColor;
    [SerializeField] private Color titleColor;
    [SerializeField] private Color iconTintColor;
    [SerializeField] private Color glowColor;
    [SerializeField] [Min(0.1f)] private float glowScaleMultiplier;
    [SerializeField] private Color shadowColor;

    public CardQualityPresentationProfile(
        CardQuality quality,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey,
        Color backgroundColor,
        Color borderColor,
        Color titleColor,
        Color iconTintColor,
        Color glowColor,
        float glowScaleMultiplier,
        Color shadowColor)
    {
        this.quality = quality;
        this.presentationKey = presentationKey;
        this.revealSfxKey = revealSfxKey;
        this.selectSfxKey = selectSfxKey;
        this.backgroundColor = backgroundColor;
        this.borderColor = borderColor;
        this.titleColor = titleColor;
        this.iconTintColor = iconTintColor;
        this.glowColor = glowColor;
        this.glowScaleMultiplier = glowScaleMultiplier;
        this.shadowColor = shadowColor;
    }

    public CardQuality Quality => quality;
    public string PresentationKey => presentationKey;
    public AudioSfxKey RevealSfxKey => revealSfxKey;
    public AudioSfxKey SelectSfxKey => selectSfxKey;
    public Color BackgroundColor => backgroundColor;
    public Color BorderColor => borderColor;
    public Color TitleColor => titleColor;
    public Color IconTintColor => iconTintColor;
    public Color GlowColor => glowColor;
    public float GlowScaleMultiplier => glowScaleMultiplier;
    public Color ShadowColor => shadowColor;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(presentationKey))
        {
            presentationKey = quality.ToString();
        }

        if (glowScaleMultiplier <= 0f)
        {
            glowScaleMultiplier = ResolveDefaultGlowScaleMultiplier(quality);
        }
    }

    private static float ResolveDefaultGlowScaleMultiplier(CardQuality quality)
    {
        return quality switch
        {
            CardQuality.Rare => 1.18f,
            CardQuality.Epic => 1.38f,
            CardQuality.Legendary => 1.62f,
            _ => 1f
        };
    }
}
