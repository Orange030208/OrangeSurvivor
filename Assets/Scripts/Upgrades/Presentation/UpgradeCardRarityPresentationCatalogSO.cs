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
                    new Color(0.12f, 0.28f, 0.75f),
                    new Color(0.72f, 0.92f, 1f),
                    0.85f,
                    0.7f,
                    10f,
                    1.1f),
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
                    new Color(0.36f, 0.12f, 0.72f),
                    new Color(1f, 0.78f, 1f),
                    1.15f,
                    1f,
                    15f,
                    1.6f),
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
                    new Color(0.85f, 0.18f, 0.1f),
                    new Color(1f, 0.95f, 0.62f),
                    1.45f,
                    1.35f,
                    18f,
                    2.1f),
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
                    new Color(0.32f, 0.46f, 0.4f),
                    new Color(0.92f, 1f, 0.96f),
                    0.45f,
                    0.35f,
                    8f,
                    0.8f),
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
        float energyDensity,
        float pulseSpeed)
    {
        return new[]
        {
            UpgradeCardShaderParameter.Float("_Rarity", (float)rarity),
            UpgradeCardShaderParameter.Float("_EffectIntensity", effectIntensity, true),
            UpgradeCardShaderParameter.Color("_PrimaryColor", primaryColor),
            UpgradeCardShaderParameter.Color("_SecondaryColor", secondaryColor),
            UpgradeCardShaderParameter.Color("_AccentColor", accentColor),
            UpgradeCardShaderParameter.Float("_GlowIntensity", effectIntensity, true),
            UpgradeCardShaderParameter.Float("_PixelGrid", 48f),
            UpgradeCardShaderParameter.Float("_FlowSpeed", flowSpeed),
            UpgradeCardShaderParameter.Float("_BorderWidth", 0.08f),
            UpgradeCardShaderParameter.Float("_BorderGlow", borderGlow, true),
            UpgradeCardShaderParameter.Float("_EnergyDensity", energyDensity),
            UpgradeCardShaderParameter.Float("_PulseSpeed", pulseSpeed)
        };
    }
}
