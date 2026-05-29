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

    public bool TryGetProfile(ContentTier tier, out CardQualityPresentationProfile profile)
    {
        return TryGetProfile(tier.ToCardQuality(), out profile);
    }

    public bool TryGetProfile(UpgradeCardRarity rarity, out CardQualityPresentationProfile profile)
    {
        return TryGetProfile(ContentTierResolver.FromUpgradeCardRarity(rarity), out profile);
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
                new Color(0.6039216f, 0.9098039f, 1f, 1f),
                null,
                0.93f),
            CardQuality.Epic => new CardQualityPresentationProfile(
                CardQuality.Epic,
                "epic",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardEpicSelected,
                new Color(0.89411765f, 0.74509805f, 1f, 1f),
                null,
                0.94f),
            CardQuality.Legendary => new CardQualityPresentationProfile(
                CardQuality.Legendary,
                "legendary",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardLegendarySelected,
                new Color(1f, 0.87058824f, 0.5803922f, 1f),
                null,
                0.95f),
            _ => new CardQualityPresentationProfile(
                CardQuality.Common,
                "common",
                AudioSfxKey.UpgradeCardReveal,
                AudioSfxKey.UpgradeCardCommonSelected,
                new Color(0.9098039f, 0.9254902f, 0.95686275f, 1f),
                null,
                0.92f)
        };
    }
}
