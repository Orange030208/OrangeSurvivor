using System.Collections;
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
    [Min(0f)]
    [SerializeField] private float coinCollectionSettleDelay = 0.5f;

    private WaveFlowRuleService waveFlowRuleService;
    private Coroutine decisionRoutine;

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

        if (decisionRoutine != null)
        {
            StopCoroutine(decisionRoutine);
            decisionRoutine = null;
        }
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent eventData)
    {
        player = eventData.Player;
    }

    private void OnWaveFlowDecisionRequested(WaveFlowDecisionRequestedEvent eventData)
    {
        if (decisionRoutine != null)
        {
            StopCoroutine(decisionRoutine);
        }

        decisionRoutine = StartCoroutine(RunWaveEndFlow(eventData));
    }

    private IEnumerator RunWaveEndFlow(WaveFlowDecisionRequestedEvent eventData)
    {
        TryBindPlayer();
        GameEventBus.Publish(new DefeatAllEnemiesRequestedEvent());
        if (StartCollectingExistingCoins())
        {
            yield return WaitForAllCoinsCollected();
        }

        if (coinCollectionSettleDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(coinCollectionSettleDelay);
        }

        GameState nextState = waveFlowRuleService.ResolveNextState(eventData, player);
        if (nextState == GameState.StageComplete)
        {
            GameEventBus.Publish<AllWavesCompletedEvent>();
        }

        GameEventBus.Publish(new WaveFlowDecisionEvent(nextState));
        decisionRoutine = null;
    }

    private bool StartCollectingExistingCoins()
    {
        if (player == null)
        {
            if (FindFirstObjectByType<Coin>() != null)
            {
                Debug.LogWarning("[WaveFlowCoordinator] Player is missing, skipping wave-end coin collection.");
            }

            return false;
        }

        Coin[] coins = FindObjectsByType<Coin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] == null)
            {
                continue;
            }

            coins[i].TryCollect(player);
        }

        return true;
    }

    private static IEnumerator WaitForAllCoinsCollected()
    {
        while (FindFirstObjectByType<Coin>() != null)
        {
            yield return null;
        }
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
