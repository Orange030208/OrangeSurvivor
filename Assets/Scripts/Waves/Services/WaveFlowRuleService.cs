using UnityEngine;

public class WaveFlowRuleService
{
    public GameState ResolveNextState(WaveFlowDecisionRequestedEvent eventData, Player player)
    {
        if (!eventData.WaveCompletedEvent.HasNextWave)
        {
            return GameState.StageComplete;
        }

        WaveFlowSnapshot flowSnapshot = eventData.FlowSnapshot;
        if (flowSnapshot.SkipToNextWaveImmediately)
        {
            return GameState.Game;
        }

        if (ShouldEnterTransition(flowSnapshot.TransitionMode, player))
        {
            return GameState.WaveTransition;
        }

        return flowSnapshot.ShopMode switch
        {
            WaveShopMode.AlwaysEnterShop => GameState.Shop,
            WaveShopMode.NeverEnterShop => GameState.Game,
            _ => GameState.Shop
        };
    }

    private bool ShouldEnterTransition(WaveTransitionMode transitionMode, Player player)
    {
        return transitionMode switch
        {
            WaveTransitionMode.AlwaysEnterTransition => true,
            WaveTransitionMode.NeverEnterTransition => false,
            _ => player != null && player.IsLevelUpInCurrentWave
        };
    }
}
