using System;
using UnityEngine;

[Serializable]
public struct UpgradeCardRarityPresentationProfile
{
    [SerializeField] private UpgradeCardRarity rarity;
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

    public UpgradeCardRarityPresentationProfile(
        UpgradeCardRarity rarity,
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
        this.rarity = rarity;
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

    public UpgradeCardRarity Rarity => rarity;
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
            presentationKey = rarity.ToString();
        }

        if (glowScaleMultiplier <= 0f)
        {
            glowScaleMultiplier = ResolveDefaultGlowScaleMultiplier(rarity);
        }
    }

    private static float ResolveDefaultGlowScaleMultiplier(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Rare => 1.18f,
            UpgradeCardRarity.Epic => 1.38f,
            UpgradeCardRarity.Legendary => 1.62f,
            _ => 1f
        };
    }
}
