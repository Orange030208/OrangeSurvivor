using UnityEngine;

/// <summary>
/// 波次奖励兑现器：
/// - 监听波次奖励事件；
/// - 下发金币奖励与宝箱次数；
/// - 不直接决定后续状态跳转。
/// </summary>
public class WaveRewardManager : MonoBehaviour
{
    private WaveRewardService waveRewardService;

    private void Awake()
    {
        waveRewardService = new WaveRewardService();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveRewardGrantedEvent>(OnWaveRewardGranted);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveRewardGrantedEvent>(OnWaveRewardGranted);
    }

    private void OnWaveRewardGranted(WaveRewardGrantedEvent eventData)
    {
        WaveRewardGrantResult grantResult = waveRewardService.Grant(eventData.RewardSnapshot);
        WaveRewardSnapshot resolvedReward = new WaveRewardSnapshot(
            grantResult.GrantedGold,
            grantResult.GrantedChestCount,
            grantResult.GrantShopEntry);
        WaveFlowSnapshot resolvedFlow = waveRewardService.ResolveFlowSnapshot(resolvedReward, eventData.FlowSnapshot);
        GameEventBus.Publish(new WaveFlowDecisionRequestedEvent(
            eventData.WaveCompletedEvent,
            resolvedReward,
            resolvedFlow));
    }
}
