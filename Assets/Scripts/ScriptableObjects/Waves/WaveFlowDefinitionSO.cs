using UnityEngine;

/// <summary>
/// 波次结束后的流程配置。
/// 用于描述该波结束后是否进过渡页、商店，或直接进入下一波。
/// </summary>
[CreateAssetMenu(fileName = "Wave Flow Definition", menuName = "SO/Waves/Wave Flow Definition", order = 0)]
public class WaveFlowDefinitionSO : ScriptableObject
{
    [Header("Flow")]
    // UsePlayerUpgradeState 表示交给玩家本波是否升级来决定是否进入过渡页。
    [SerializeField] private WaveTransitionMode transitionMode = WaveTransitionMode.UsePlayerUpgradeState;
    // UseRewardGate 表示交给奖励配置中的商店放行结果决定是否进入商店。
    [SerializeField] private WaveShopMode shopMode = WaveShopMode.UseRewardGate;
    [SerializeField] private bool skipToNextWaveImmediately;

    public WaveTransitionMode TransitionMode => transitionMode;
    public WaveShopMode ShopMode => shopMode;
    public bool SkipToNextWaveImmediately => skipToNextWaveImmediately;
}
