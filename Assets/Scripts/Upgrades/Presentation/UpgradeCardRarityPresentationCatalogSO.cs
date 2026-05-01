using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Upgrade Card Rarity Presentation Catalog",
    menuName = ScriptableObjectMenuPaths.UPGRADE_CARD_RARITY_PRESENTATION_CATALOG,
    order = 0)]
public sealed class UpgradeCardRarityPresentationCatalogSO : ScriptableObject
{
    [SerializeField] private UpgradeCardRarityPresentationProfile[] profiles = Array.Empty<UpgradeCardRarityPresentationProfile>();

    public bool TryGetProfile(UpgradeCardRarity rarity, out UpgradeCardRarityPresentationProfile profile)
    {
        if (profiles != null)
        {
            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i].Rarity == rarity)
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

    public void InitializeRuntime(UpgradeCardRarityPresentationProfile[] runtimeProfiles)
    {
        profiles = runtimeProfiles ?? Array.Empty<UpgradeCardRarityPresentationProfile>();
        ValidateProfiles();
    }

    private void OnValidate()
    {
        ValidateProfiles();
    }

    private void ValidateProfiles()
    {
        profiles ??= Array.Empty<UpgradeCardRarityPresentationProfile>();
        for (int i = 0; i < profiles.Length; i++)
        {
            profiles[i].Validate();
        }
    }

    public static UpgradeCardRarityPresentationProfile CreateBuiltinProfile(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Rare => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Rare,
                "rare",
                AudioSfxKey.UpgradeCardRareReveal,
                AudioSfxKey.UpgradeCardRareSelected,
                new Color(0.078431375f, 0.19607843f, 0.26666668f, 0.8784314f),
                new Color(0.3372549f, 0.8235294f, 1f, 1f),
                new Color(0.6039216f, 0.9098039f, 1f, 1f),
                new Color(0.83137256f, 0.95686275f, 1f, 1f),
                new Color(0.3137255f, 0.8235294f, 1f, 0.50980395f),
                new Color(0f, 0.09411765f, 0.14901961f, 1f)),
            UpgradeCardRarity.Epic => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Epic,
                "epic",
                AudioSfxKey.UpgradeCardEpicReveal,
                AudioSfxKey.UpgradeCardEpicSelected,
                new Color(0.21176471f, 0.13725491f, 0.30588236f, 0.8784314f),
                new Color(0.7607843f, 0.47058824f, 1f, 1f),
                new Color(0.89411765f, 0.74509805f, 1f, 1f),
                new Color(0.9490196f, 0.8784314f, 1f, 1f),
                new Color(0.7607843f, 0.47058824f, 1f, 0.5686275f),
                new Color(0.10980392f, 0.047058824f, 0.17254902f, 1f)),
            UpgradeCardRarity.Legendary => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Legendary,
                "legendary",
                AudioSfxKey.UpgradeCardLegendaryReveal,
                AudioSfxKey.UpgradeCardLegendarySelected,
                new Color(0.30588236f, 0.20392157f, 0.08627451f, 0.9019608f),
                new Color(1f, 0.7607843f, 0.32156864f, 1f),
                new Color(1f, 0.87058824f, 0.5803922f, 1f),
                new Color(1f, 0.93333334f, 0.7921569f, 1f),
                new Color(1f, 0.7372549f, 0.29803923f, 0.6666667f),
                new Color(0.1882353f, 0.10980392f, 0.019607844f, 1f)),
            _ => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Common,
                "common",
                AudioSfxKey.UpgradeCardCommonReveal,
                AudioSfxKey.WoodenButtonClicked,
                new Color(0.13333334f, 0.14901961f, 0.1764706f, 0.8627451f),
                new Color(0.6039216f, 0.6509804f, 0.7137255f, 1f),
                new Color(0.9098039f, 0.9254902f, 0.95686275f, 1f),
                new Color(0.9647059f, 0.972549f, 0.9882353f, 1f),
                new Color(0.5882353f, 0.6509804f, 0.74509805f, 0.3529412f),
                new Color(0f, 0f, 0f, 1f))
        };
    }
}
