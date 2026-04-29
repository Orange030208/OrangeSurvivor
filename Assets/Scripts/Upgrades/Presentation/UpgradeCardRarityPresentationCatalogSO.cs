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
                BuildShaderParameters(
                    UpgradeCardRarity.Rare,
                    0.7f,
                    new Color(0.35f, 0.65f, 1f),
                    new Color(0.06f, 0.16f, 0.38f),
                    new Color(0.66f, 0.9f, 1f),
                    0.48f,
                    0.42f,
                    0.18f,
                    0.42f,
                    0.08f),
                0.7f,
                AudioSfxKey.UpgradeCardRareReveal,
                AudioSfxKey.UpgradeCardRareSelected),
            UpgradeCardRarity.Epic => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Epic,
                "epic",
                BuildShaderParameters(
                    UpgradeCardRarity.Epic,
                    1f,
                    new Color(0.76f, 0.38f, 1f),
                    new Color(0.22f, 0.08f, 0.45f),
                    new Color(1f, 0.72f, 1f),
                    0.68f,
                    0.52f,
                    0.24f,
                    0.62f,
                    0.18f),
                1f,
                AudioSfxKey.UpgradeCardEpicReveal,
                AudioSfxKey.UpgradeCardEpicSelected),
            UpgradeCardRarity.Legendary => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Legendary,
                "legendary",
                BuildShaderParameters(
                    UpgradeCardRarity.Legendary,
                    1.35f,
                    new Color(1f, 0.76f, 0.25f),
                    new Color(0.52f, 0.16f, 0.05f),
                    new Color(1f, 0.92f, 0.48f),
                    0.86f,
                    0.62f,
                    0.28f,
                    0.76f,
                    0.34f),
                1.35f,
                AudioSfxKey.UpgradeCardLegendaryReveal,
                AudioSfxKey.UpgradeCardLegendarySelected),
            _ => new UpgradeCardRarityPresentationProfile(
                UpgradeCardRarity.Common,
                "common",
                BuildShaderParameters(
                    UpgradeCardRarity.Common,
                    0.35f,
                    new Color(0.82f, 0.9f, 0.86f),
                    new Color(0.24f, 0.32f, 0.29f),
                    new Color(0.9f, 1f, 0.95f),
                    0.22f,
                    0.28f,
                    0.08f,
                    0.12f,
                    0f),
                0.35f,
                AudioSfxKey.UpgradeCardCommonReveal,
                AudioSfxKey.WoodenButtonClicked)
        };
    }

    private static UpgradeCardShaderParameter[] BuildShaderParameters(
        UpgradeCardRarity rarity,
        float effectIntensity,
        Color primaryColor,
        Color secondaryColor,
        Color accentColor,
        float borderGlow,
        float flowSpeed,
        float patternIntensity,
        float sweepIntensity,
        float pulseIntensity)
    {
        return new[]
        {
            UpgradeCardShaderParameter.Float("_Rarity", (float)rarity),
            UpgradeCardShaderParameter.Float("_EffectIntensity", effectIntensity, true),
            UpgradeCardShaderParameter.Color("_PrimaryColor", primaryColor),
            UpgradeCardShaderParameter.Color("_SecondaryColor", secondaryColor),
            UpgradeCardShaderParameter.Color("_AccentColor", accentColor),
            UpgradeCardShaderParameter.Float("_GlowIntensity", effectIntensity, true),
            UpgradeCardShaderParameter.Float("_FlowSpeed", flowSpeed),
            UpgradeCardShaderParameter.Float("_BorderWidth", 0.075f),
            UpgradeCardShaderParameter.Float("_BorderGlow", borderGlow, true),
            UpgradeCardShaderParameter.Float("_PulseSpeed", 0.9f),
            UpgradeCardShaderParameter.Float("_PatternIntensity", patternIntensity),
            UpgradeCardShaderParameter.Float("_SweepIntensity", sweepIntensity),
            UpgradeCardShaderParameter.Float("_PulseIntensity", pulseIntensity)
        };
    }
}
