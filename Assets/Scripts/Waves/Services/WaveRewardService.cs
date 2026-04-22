using UnityEngine;

public class WaveRewardService
{
    public WaveRewardGrantResult Grant(WaveRewardSnapshot rewardSnapshot, CurrencyWallet wallet)
    {
        int grantedGold = 0;
        int grantedChestCount = 0;

        if (rewardSnapshot.GoldReward > 0)
        {
            grantedGold = rewardSnapshot.GoldReward;
            wallet?.ChangeAmount(grantedGold);
        }

        for (int i = 0; i < rewardSnapshot.ChestRewardCount; i++)
        {
            GameEventBus.Publish(new ChestCollectedEvent());
            grantedChestCount++;
        }

        return new WaveRewardGrantResult(grantedGold, grantedChestCount, rewardSnapshot.GrantShopEntry);
    }

    public WaveFlowSnapshot ResolveFlowSnapshot(WaveRewardSnapshot rewardSnapshot, WaveFlowSnapshot configuredFlow)
    {
        WaveShopMode resolvedShopMode = configuredFlow.ShopMode;
        if (configuredFlow.ShopMode == WaveShopMode.UseRewardGate && !rewardSnapshot.GrantShopEntry)
        {
            resolvedShopMode = WaveShopMode.NeverEnterShop;
        }

        return new WaveFlowSnapshot(
            configuredFlow.TransitionMode,
            resolvedShopMode,
            configuredFlow.SkipToNextWaveImmediately);
    }
}
