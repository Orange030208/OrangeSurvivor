using UnityEngine;

public readonly struct WaveRewardGrantResult
{
    public readonly int GrantedGold;
    public readonly int GrantedChestCount;
    public readonly bool GrantShopEntry;

    public WaveRewardGrantResult(int grantedGold, int grantedChestCount, bool grantShopEntry)
    {
        GrantedGold = grantedGold;
        GrantedChestCount = grantedChestCount;
        GrantShopEntry = grantShopEntry;
    }
}
