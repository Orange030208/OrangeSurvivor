using UnityEngine;

/// <summary>
/// 波次奖励配置。
/// 只描述奖励内容本身，不直接决定奖励发放后的流程跳转。
/// </summary>
[CreateAssetMenu(fileName = "Wave Reward Definition", menuName = ScriptableObjectMenuPaths.WAVE_REWARD_DEFINITION, order = 0)]
public class WaveRewardDefinitionSO : ScriptableObject
{
    private const int MIN_REWARD_VALUE = 0;

    [Header("Reward")]
    [SerializeField] private int goldReward;
    [SerializeField] private int chestRewardCount;
    // 仅作为 UseRewardGate 模式下的商店放行条件。
    [SerializeField] private bool grantShopEntry = true;

    public int GoldReward => Mathf.Max(MIN_REWARD_VALUE, goldReward);
    public int ChestRewardCount => Mathf.Max(MIN_REWARD_VALUE, chestRewardCount);
    public bool GrantShopEntry => grantShopEntry;

    private void OnValidate()
    {
        goldReward = Mathf.Max(MIN_REWARD_VALUE, goldReward);
        chestRewardCount = Mathf.Max(MIN_REWARD_VALUE, chestRewardCount);
    }
}
