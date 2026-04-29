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
    [SerializeField] private Player player;

    private WaveRewardService waveRewardService;
    private PropertiesManager propertiesManager;

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
        player = eventData.Player;
        wallet = player != null ? player.GetComponent<CurrencyWallet>() : null;
        propertiesManager = player != null ? player.GetComponent<PropertiesManager>() : null;
    }

    private void OnWaveRewardGranted(WaveRewardGrantedEvent eventData)
    {
        WaveRewardSnapshot rewardSnapshot = ApplyUpgradeRewardBonus(eventData.RewardSnapshot);
        WaveRewardGrantResult grantResult = waveRewardService.Grant(rewardSnapshot, wallet);
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

    private WaveRewardSnapshot ApplyUpgradeRewardBonus(WaveRewardSnapshot rewardSnapshot)
    {
        int extraGoldReward = ResolveWaveGoldRewardBonus();
        if (extraGoldReward <= 0)
        {
            return rewardSnapshot;
        }

        int goldReward = Mathf.Max(0, rewardSnapshot.GoldReward) + extraGoldReward;
        return new WaveRewardSnapshot(goldReward, rewardSnapshot.ChestRewardCount, rewardSnapshot.GrantShopEntry);
    }

    private int ResolveWaveGoldRewardBonus()
    {
        if (propertiesManager == null)
        {
            TryBindPlayerReferences();
        }

        float bonusGold = propertiesManager != null
            ? Mathf.Max(0f, propertiesManager.GetPropValue(PropType.WaveGoldRewardBonus))
            : 0f;
        return Mathf.RoundToInt(bonusGold);
    }

    private void TryBindWallet()
    {
        if (wallet != null && propertiesManager != null)
        {
            return;
        }

        TryBindPlayerReferences();
    }

    private void TryBindPlayerReferences()
    {
        player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return;
        }

        if (wallet == null)
        {
            wallet = player.GetComponent<CurrencyWallet>();
        }

        if (propertiesManager == null)
        {
            propertiesManager = player.GetComponent<PropertiesManager>();
        }
    }
}
