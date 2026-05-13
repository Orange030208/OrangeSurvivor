using UnityEngine;

public readonly struct CoinRewardData
{
    public int GoldValue { get; }

    public bool HasAnyReward => GoldValue > 0;

    public CoinRewardData(int goldValue)
    {
        GoldValue = Mathf.Max(0, goldValue);
    }
}
