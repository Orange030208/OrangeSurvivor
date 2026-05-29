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
        Color mainColor,
        Sprite backgroundSprite,
        float backgroundAlpha)
    {
        profile = new CardQualityPresentationProfile(
            ContentTierResolver.FromUpgradeCardRarity(rarity).ToCardQuality(),
            presentationKey,
            revealSfxKey,
            selectSfxKey,
            mainColor,
            backgroundSprite,
            backgroundAlpha);
    }

    public UpgradeCardRarity Rarity => (UpgradeCardRarity)(int)profile.Quality;
    public string PresentationKey => profile.PresentationKey;
    public AudioSfxKey RevealSfxKey => profile.RevealSfxKey;
    public AudioSfxKey SelectSfxKey => profile.SelectSfxKey;
    public Color MainColor => profile.MainColor;
    public Color TitleColor => profile.TitleColor;
    public Sprite BackgroundSprite => profile.BackgroundSprite;
    public Sprite IconFrameSprite => profile.IconFrameSprite;
    public Sprite IconBackgroundSprite => profile.IconBackgroundSprite;
    public float BackgroundAlpha => profile.BackgroundAlpha;
    public float ShadowScale => profile.ShadowScale;
    public float GlowScale => profile.GlowScale;

    public void Validate()
    {
        profile.Validate();
    }
}
