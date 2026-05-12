using UnityEngine;

public readonly struct CoinRewardData
{
    public int GoldValue { get; }
    public int ExperienceValue { get; }

    public bool HasAnyReward => GoldValue > 0 || ExperienceValue > 0;

    public CoinRewardData(int goldValue, int experienceValue)
    {
        GoldValue = Mathf.Max(0, goldValue);
        ExperienceValue = Mathf.Max(0, experienceValue);
    }
}
