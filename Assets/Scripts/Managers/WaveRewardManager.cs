using UnityEngine;

/// <summary>
/// 波次奖励兑现器：
/// - 监听波次奖励事件；
/// - 下发金币奖励与宝箱次数；
/// - 不直接决定后续状态跳转。
/// </summary>
public class WaveRewardManager : MonoBehaviour
{
    [SerializeField] private CurrencyWallet wallet;

    private WaveRewardService waveRewardService;

    private void Awake()
    {
        waveRewardService = new WaveRewardService();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveRewardGrantedEvent>(OnWaveRewardGranted);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        TryBindWallet();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveRewardGrantedEvent>(OnWaveRewardGranted);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        wallet = eventData.Player != null ? eventData.Player.GetComponent<CurrencyWallet>() : null;
    }

    private void OnWaveRewardGranted(WaveRewardGrantedEvent eventData)
    {
        WaveRewardGrantResult grantResult = waveRewardService.Grant(eventData.RewardSnapshot, wallet);
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

    private void TryBindWallet()
    {
        if (wallet != null)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return;
        }

        wallet = player.GetComponent<CurrencyWallet>();
    }
}
