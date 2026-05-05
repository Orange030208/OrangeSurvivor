using UnityEngine;

/// <summary>
/// 单波配置资源。
/// 描述该波的持续时间、刷怪计划以及刷怪位置策略。
/// </summary>
[CreateAssetMenu(fileName = "Wave Definition", menuName = ScriptableObjectMenuPaths.WAVE_DEFINITION, order = 0)]
public class WaveDefinitionSO : ScriptableObject
{
    private const float MIN_DURATION = 1f;

    [Header("Identity")]
    [SerializeField] private string waveId = "Wave_001";
    [SerializeField] private string displayName = "Wave 1";

    [Header("Runtime")]
    [SerializeField] private float duration = 30f;
    [SerializeField] private SpawnLocationPolicySO spawnLocationPolicy;
    [SerializeField] private WaveSpawnPlan[] spawnPlans;

    public string WaveId => waveId;
    public string DisplayName => displayName;
    public float Duration => Mathf.Max(MIN_DURATION, duration);
    public SpawnLocationPolicySO SpawnLocationPolicy => spawnLocationPolicy;
    public WaveSpawnPlan[] SpawnPlans => spawnPlans;

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
        spawnPlans ??= System.Array.Empty<WaveSpawnPlan>();
    }
}
