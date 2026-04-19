using System;
using UnityEngine;

[Serializable]
public struct WaveRewardSnapshot
{
    public int GoldReward;
    public int ChestRewardCount;
    public bool GrantShopEntry;

    public WaveRewardSnapshot(int goldReward, int chestRewardCount, bool grantShopEntry)
    {
        GoldReward = goldReward;
        ChestRewardCount = chestRewardCount;
        GrantShopEntry = grantShopEntry;
    }

    public static WaveRewardSnapshot CreateDefault()
    {
        return new WaveRewardSnapshot(0, 0, true);
    }
}
