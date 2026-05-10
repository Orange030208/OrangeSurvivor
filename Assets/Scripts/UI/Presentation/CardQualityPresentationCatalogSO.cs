using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Card Quality Presentation Catalog",
    menuName = ScriptableObjectMenuPaths.CARD_QUALITY_PRESENTATION_CATALOG,
    order = 0)]
public class CardQualityPresentationCatalogSO : ScriptableObject
{
    [SerializeField] private CardQualityPresentationProfile[] profiles = Array.Empty<CardQualityPresentationProfile>();

    public bool TryGetProfile(CardQuality quality, out CardQualityPresentationProfile profile)
    {
        if (profiles != null)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i].Quality == quality)
                {
                    profile = profiles[i];
                    profile.Validate();
                    return true;
                }
            }
        }

        profile = default;
        return false;
    }

    public bool TryGetProfile(UpgradeCardRarity rarity, out CardQualityPresentationProfile profile)
    {
        return TryGetProfile(CardQualityResolver.FromUpgradeCardRarity(rarity), out profile);
    }

    public void InitializeRuntime(CardQualityPresentationProfile[] runtimeProfiles)
    {
        profiles = runtimeProfiles ?? Array.Empty<CardQualityPresentationProfile>();
        ValidateProfiles();
    }

    private void OnValidate()
    {
        ValidateProfiles();
    }

    private void ValidateProfiles()
    {
        profiles ??= Array.Empty<CardQualityPresentationProfile>();
        for (int i = 0; i < profiles.Length; i++)
        {
            profiles[i].Validate();
        }
    }

    public static CardQualityPresentationProfile CreateBuiltinProfile(CardQuality quality)
    {
        return quality switch
        {
            CardQuality.Rare => new CardQualityPresentationProfile(
                CardQuality.Rare,
                "rare",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardRareSelected,
                new Color(0.078431375f, 0.19607843f, 0.26666668f, 0.8784314f),
                new Color(0.3372549f, 0.8235294f, 1f, 1f),
                new Color(0.6039216f, 0.9098039f, 1f, 1f),
                new Color(0.83137256f, 0.95686275f, 1f, 1f),
                new Color(0.3137255f, 0.8235294f, 1f, 0.50980395f),
                1.18f,
                new Color(0f, 0.09411765f, 0.14901961f, 1f)),
            CardQuality.Epic => new CardQualityPresentationProfile(
                CardQuality.Epic,
                "epic",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardEpicSelected,
                new Color(0.21176471f, 0.13725491f, 0.30588236f, 0.8784314f),
                new Color(0.7607843f, 0.47058824f, 1f, 1f),
                new Color(0.89411765f, 0.74509805f, 1f, 1f),
                new Color(0.9490196f, 0.8784314f, 1f, 1f),
                new Color(0.7607843f, 0.47058824f, 1f, 0.5686275f),
                1.38f,
                new Color(0.10980392f, 0.047058824f, 0.17254902f, 1f)),
            CardQuality.Legendary => new CardQualityPresentationProfile(
                CardQuality.Legendary,
                "legendary",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardLegendarySelected,
                new Color(0.30588236f, 0.20392157f, 0.08627451f, 0.9019608f),
                new Color(1f, 0.7607843f, 0.32156864f, 1f),
                new Color(1f, 0.87058824f, 0.5803922f, 1f),
                new Color(1f, 0.93333334f, 0.7921569f, 1f),
                new Color(1f, 0.7372549f, 0.29803923f, 0.6666667f),
                1.62f,
                new Color(0.1882353f, 0.10980392f, 0.019607844f, 1f)),
            _ => new CardQualityPresentationProfile(
                CardQuality.Common,
                "common",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardCommonSelected,
                new Color(0.13333334f, 0.14901961f, 0.1764706f, 0.8627451f),
                new Color(0.6039216f, 0.6509804f, 0.7137255f, 1f),
                new Color(0.9098039f, 0.9254902f, 0.95686275f, 1f),
                new Color(0.9647059f, 0.972549f, 0.9882353f, 1f),
                new Color(0.5882353f, 0.6509804f, 0.74509805f, 0.3529412f),
                1f,
                new Color(0f, 0f, 0f, 1f))
        };
    }
}
