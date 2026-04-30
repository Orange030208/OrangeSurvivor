using System;
using UnityEngine;

[Serializable]
public struct UpgradeCardRarityPresentationProfile
{
    [SerializeField] private UpgradeCardRarity rarity;
    [SerializeField] private string presentationKey;
    [SerializeField] private AudioSfxKey revealSfxKey;
    [SerializeField] private AudioSfxKey selectSfxKey;

    public UpgradeCardRarityPresentationProfile(
        UpgradeCardRarity rarity,
        string presentationKey,
        AudioSfxKey revealSfxKey,
        AudioSfxKey selectSfxKey)
    {
        this.rarity = rarity;
        this.presentationKey = presentationKey;
        this.revealSfxKey = revealSfxKey;
        this.selectSfxKey = selectSfxKey;
    }

    public UpgradeCardRarity Rarity => rarity;
    public string PresentationKey => presentationKey;
    public AudioSfxKey RevealSfxKey => revealSfxKey;
    public AudioSfxKey SelectSfxKey => selectSfxKey;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(presentationKey))
        {
            presentationKey = rarity.ToString();
        }
    }
}
