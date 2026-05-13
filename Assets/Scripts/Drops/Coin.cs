using UnityEngine;

public class Coin : AbsorbableCollection
{
    [SerializeField] private int goldValue = 1;

    public void ConfigureReward(CoinRewardData rewardData)
    {
        goldValue = rewardData.GoldValue;
    }

    protected override void OnCollected(IEntity entity)
    {
        CurrencyWallet currencyWallet = entity.Transform.GetComponent<CurrencyWallet>();
        if (currencyWallet != null && goldValue > 0)
        {
            currencyWallet.ChangeAmount(goldValue);
        }
    }
}
