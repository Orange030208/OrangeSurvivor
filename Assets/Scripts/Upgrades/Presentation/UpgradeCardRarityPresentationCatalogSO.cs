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
                    return true;
                }
            }
        }

        profile = GetDefaultProfile(rarity);
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

    public static UpgradeCardRarityPresentationProfile GetDefaultProfile(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Rare => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Rare,
                "rare",
                AudioSfxKey.UpgradeCardRareReveal,
                AudioSfxKey.UpgradeCardRareSelected),
            UpgradeCardRarity.Epic => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Epic,
                "epic",
                AudioSfxKey.UpgradeCardEpicReveal,
                AudioSfxKey.UpgradeCardEpicSelected),
            UpgradeCardRarity.Legendary => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Legendary,
                "legendary",
                AudioSfxKey.UpgradeCardLegendaryReveal,
                AudioSfxKey.UpgradeCardLegendarySelected),
            _ => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Common,
                "common",
                AudioSfxKey.UpgradeCardCommonReveal,
                AudioSfxKey.WoodenButtonClicked)
        };
    }
}
