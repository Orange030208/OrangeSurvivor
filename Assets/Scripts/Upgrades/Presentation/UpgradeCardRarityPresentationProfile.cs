using System;
using UnityEngine;

[Serializable]
public struct UpgradeCardRarityPresentationProfile
{
    [SerializeField] private CardQualityPresentationProfile profile;

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
        profile = new CardQualityPresentationProfile(
            CardQualityResolver.FromUpgradeCardRarity(rarity),
            presentationKey,
            revealSfxKey,
            selectSfxKey,
            backgroundColor,
            borderColor,
            titleColor,
            iconTintColor,
            glowColor,
            glowScaleMultiplier,
            shadowColor);
    }

    public UpgradeCardRarity Rarity => (UpgradeCardRarity)(int)profile.Quality;
    public string PresentationKey => profile.PresentationKey;
    public AudioSfxKey RevealSfxKey => profile.RevealSfxKey;
    public AudioSfxKey SelectSfxKey => profile.SelectSfxKey;
    public Color BackgroundColor => profile.BackgroundColor;
    public Color BorderColor => profile.BorderColor;
    public Color TitleColor => profile.TitleColor;
    public Color IconTintColor => profile.IconTintColor;
    public Color GlowColor => profile.GlowColor;
    public float GlowScaleMultiplier => profile.GlowScaleMultiplier;
    public Color ShadowColor => profile.ShadowColor;

    public void Validate()
    {
        profile.Validate();
    }
}
