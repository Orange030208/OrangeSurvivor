using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade Card Pool", menuName = ScriptableObjectMenuPaths.UPGRADE_CARD_POOL, order = 0)]
public class UpgradeCardPoolSO : ScriptableObject
{
    private const int MIN_OPTION_COUNT = 1;
    private const int DEFAULT_OPTION_COUNT = 3;

    [SerializeField] private List<UpgradeCardSO> cards = new();
    [SerializeField] private int optionCount = DEFAULT_OPTION_COUNT;
    [SerializeField] private float matchingTagWeightBonus = 0.15f;
    [SerializeField] private float previousOfferWeightMultiplier = 0.5f;
    [SerializeField] private UpgradeRarityWeightByWave[] rarityWeights =
    {
        new(1, 80, 20, 0, 0),
        new(3, 60, 30, 10, 0),
        new(6, 45, 35, 17, 3)
    };

    public IReadOnlyList<UpgradeCardSO> Cards => cards;
    public int OptionCount => Mathf.Max(MIN_OPTION_COUNT, optionCount);
    public float MatchingTagWeightBonus => Mathf.Max(0f, matchingTagWeightBonus);
    public float PreviousOfferWeightMultiplier => Mathf.Clamp01(previousOfferWeightMultiplier);
    public IReadOnlyList<UpgradeRarityWeightByWave> RarityWeights => rarityWeights;

    private void OnValidate()
    {
        optionCount = Mathf.Max(MIN_OPTION_COUNT, optionCount);
        matchingTagWeightBonus = Mathf.Max(0f, matchingTagWeightBonus);
        previousOfferWeightMultiplier = Mathf.Clamp01(previousOfferWeightMultiplier);
    }

    public UpgradeRarityWeightByWave ResolveRarityWeights(int waveNumber)
    {
        if (rarityWeights == null || rarityWeights.Length == 0)
        {
            return new UpgradeRarityWeightByWave(1, 80, 20, 0, 0);
        }

        int normalizedWave = Mathf.Max(1, waveNumber);
        UpgradeRarityWeightByWave selected = rarityWeights[0];
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            UpgradeRarityWeightByWave candidate = rarityWeights[i];
            if (candidate.MinWave <= normalizedWave && candidate.MinWave >= selected.MinWave)
            {
                selected = candidate;
            }
        }

        return selected;
    }

    public void InitializeRuntime(IReadOnlyList<UpgradeCardSO> runtimeCards)
    {
        cards = runtimeCards != null ? new List<UpgradeCardSO>(runtimeCards) : new List<UpgradeCardSO>();
        optionCount = DEFAULT_OPTION_COUNT;
        matchingTagWeightBonus = 0.15f;
        previousOfferWeightMultiplier = 0.5f;
        rarityWeights = new[]
        {
            new UpgradeRarityWeightByWave(1, 80, 20, 0, 0),
            new UpgradeRarityWeightByWave(3, 60, 30, 10, 0),
            new UpgradeRarityWeightByWave(6, 45, 35, 17, 3)
        };
    }
}
