using System;
using UnityEngine;

[Serializable]
public struct WaveFlowSnapshot
{
    public WaveTransitionMode TransitionMode;
    public WaveShopMode ShopMode;
    public bool SkipToNextWaveImmediately;

    public WaveFlowSnapshot(WaveTransitionMode transitionMode, WaveShopMode shopMode, bool skipToNextWaveImmediately)
    {
        TransitionMode = transitionMode;
        ShopMode = shopMode;
        SkipToNextWaveImmediately = skipToNextWaveImmediately;
    }

    public static WaveFlowSnapshot CreateDefault()
    {
        return new WaveFlowSnapshot(WaveTransitionMode.UsePlayerUpgradeState, WaveShopMode.UseRewardGate, false);
    }
}
