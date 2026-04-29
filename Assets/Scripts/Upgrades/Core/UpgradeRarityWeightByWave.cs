using System;
using UnityEngine;

[Serializable]
public struct UpgradeRarityWeightByWave
{
    [SerializeField] private int minWave;
    [SerializeField] private int commonWeight;
    [SerializeField] private int rareWeight;
    [SerializeField] private int epicWeight;
    [SerializeField] private int legendaryWeight;

    public int MinWave => Mathf.Max(1, minWave);
    public int CommonWeight => Mathf.Max(0, commonWeight);
    public int RareWeight => Mathf.Max(0, rareWeight);
    public int EpicWeight => Mathf.Max(0, epicWeight);
    public int LegendaryWeight => Mathf.Max(0, legendaryWeight);

    public UpgradeRarityWeightByWave(
        int minWave,
        int commonWeight,
        int rareWeight,
        int epicWeight,
        int legendaryWeight)
    {
        this.minWave = Mathf.Max(1, minWave);
        this.commonWeight = Mathf.Max(0, commonWeight);
        this.rareWeight = Mathf.Max(0, rareWeight);
        this.epicWeight = Mathf.Max(0, epicWeight);
        this.legendaryWeight = Mathf.Max(0, legendaryWeight);
    }

    public int GetWeight(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => CommonWeight,
            UpgradeCardRarity.Rare => RareWeight,
            UpgradeCardRarity.Epic => EpicWeight,
            UpgradeCardRarity.Legendary => LegendaryWeight,
            _ => 0
        };
    }
}
