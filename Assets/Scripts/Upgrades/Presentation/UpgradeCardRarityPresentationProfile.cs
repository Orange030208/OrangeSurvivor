using System;
using UnityEngine;

[Serializable]
public struct UpgradeCardRarityPresentationProfile
{
    [SerializeField] private UpgradeCardRarity rarity;
    [SerializeField] private string presentationKey;
    [SerializeField] private UpgradeCardShaderParameter[] shaderParameters;
    [Range(0f, 2f)]
    [SerializeField] private float effectIntensity;
    [SerializeField] private AudioSfxKey revealSfxKey;
    [SerializeField] private AudioSfxKey selectSfxKey;

    public UpgradeCardRarityPresentationProfile(
        UpgradeCardRarity rarity,
        string presentationKey,
        UpgradeCardShaderParameter[] shaderParameters,
        float effectIntensity,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey)
    {
        this.rarity = rarity;
        this.presentationKey = presentationKey;
        this.shaderParameters = shaderParameters ?? Array.Empty<UpgradeCardShaderParameter>();
        this.effectIntensity = Mathf.Max(0f, effectIntensity);
        this.revealSfxKey = revealSfxKey;
        this.selectSfxKey = selectSfxKey;
    }

    public UpgradeCardRarity Rarity => rarity;
    public string PresentationKey => presentationKey;
    public UpgradeCardShaderParameter[] ShaderParameters => shaderParameters ?? Array.Empty<UpgradeCardShaderParameter>();
    public float EffectIntensity => Mathf.Max(0f, effectIntensity);
    public AudioSfxKey RevealSfxKey => revealSfxKey;
    public AudioSfxKey SelectSfxKey => selectSfxKey;
    public bool HasShaderParameters => ShaderParameters.Length > 0;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(presentationKey))
        {
            presentationKey = rarity.ToString();
        }

        shaderParameters ??= Array.Empty<UpgradeCardShaderParameter>();
        for (int i = 0; i < shaderParameters.Length; i++)
        {
            shaderParameters[i].Validate();
        }

        effectIntensity = Mathf.Max(0f, effectIntensity);
    }
}
