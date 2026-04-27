using UnityEngine;

/// <summary>
/// 单波配置资源。
/// 描述该波的完成条件、刷怪计划、刷怪位置策略以及奖励/流程语义。
/// </summary>
[CreateAssetMenu(fileName = "Wave Definition", menuName = ScriptableObjectMenuPaths.WAVE_DEFINITION, order = 0)]
public class WaveDefinitionSO : ScriptableObject
{
    private const float MIN_DURATION = 1f;

    [Header("Identity")]
    [SerializeField] private string waveId = "Wave_001";
    [SerializeField] private string displayName = "Wave 1";

    [Header("Runtime")]
    // 仅在计时结束类波次中直接作为完成时长使用。
    [SerializeField] private float duration = 30f;
    [SerializeField] private WaveCompletionType completionType = WaveCompletionType.DurationElapsed;
    [SerializeField] private SpawnLocationPolicySO spawnLocationPolicy;
    [SerializeField] private WaveSpawnPlan[] spawnPlans;

    [Header("Semantics")]
    [SerializeField] private WaveTag waveTags = WaveTag.Normal;
    [SerializeField] private WaveRewardDefinitionSO rewardDefinition;
    [SerializeField] private WaveFlowDefinitionSO flowDefinition;

    public string WaveId => waveId;
    public string DisplayName => displayName;
    public float Duration => Mathf.Max(MIN_DURATION, duration);
    public WaveCompletionType CompletionType => completionType;
    public SpawnLocationPolicySO SpawnLocationPolicy => spawnLocationPolicy;
    public WaveSpawnPlan[] SpawnPlans => spawnPlans;
    public WaveTag WaveTags => waveTags;
    public WaveRewardDefinitionSO RewardDefinition => rewardDefinition;
    public WaveFlowDefinitionSO FlowDefinition => flowDefinition;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(waveId))
        {
            waveId = "Wave_001";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = waveId;
        }

        duration = Mathf.Max(MIN_DURATION, duration);
    }
}
