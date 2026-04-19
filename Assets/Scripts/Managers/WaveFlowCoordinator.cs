using UnityEngine;

/// <summary>
/// 波次流程协调器：
/// - 监听波次完成与奖励结果；
/// - 计算下一步流程状态；
/// - 通过事件把决策发给 GameManager 执行。
/// </summary>
public class WaveFlowCoordinator : MonoBehaviour
{
    [SerializeField] private Player player;

    private WaveFlowRuleService waveFlowRuleService;

    private void Awake()
    {
        waveFlowRuleService = new WaveFlowRuleService();
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<WaveFlowDecisionRequestedEvent>(OnWaveFlowDecisionRequested);
        GameEventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

        TryBindPlayer();
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<WaveFlowDecisionRequestedEvent>(OnWaveFlowDecisionRequested);
        GameEventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
    }

    private void OnWaveFlowDecisionRequested(WaveFlowDecisionRequestedEvent eventData)
    {
        GameState nextState = waveFlowRuleService.ResolveNextState(eventData, player);
        if (nextState == GameState.StageComplete)
        {
            GameEventBus.Publish<AllWavesCompletedEvent>();
        }

        GameEventBus.Publish(new WaveFlowDecisionEvent(nextState));
    }

    private void TryBindPlayer()
    {
        if (player != null)
        {
            return;
        }

        player = FindFirstObjectByType<Player>();
    }
}
