using UnityEngine;

public class Coin : AbsorbableCollection
{
    private const int BASE_EXPERIENCE_VALUE = 1;

    [SerializeField] private int experienceValue = BASE_EXPERIENCE_VALUE;
    [SerializeField] private int goldValue = 1;

    public void ConfigureReward(CoinRewardData rewardData)
    {
        goldValue = rewardData.GoldValue;
        experienceValue = rewardData.ExperienceValue;
    }

    protected override void OnCollected(IEntity entity)
    {
        PlayerLevel levelComponent = entity.Transform.GetComponent<PlayerLevel>();
        if (levelComponent != null && experienceValue > 0)
        {
            levelComponent.AddXP(experienceValue);
        }
        
        CurrencyWallet currencyWallet = entity.Transform.GetComponent<CurrencyWallet>();
        if (currencyWallet != null && goldValue > 0)
        {
            currencyWallet.ChangeAmount(goldValue);
        }
    }
}
