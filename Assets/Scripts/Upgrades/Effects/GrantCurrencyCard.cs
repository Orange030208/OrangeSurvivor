using System;
using UnityEngine;

[Serializable]
public sealed class GrantCurrencyCard : FeatureEffectBase
{
    [SerializeField] private int amount = 10;

    public GrantCurrencyCard()
    {
    }

    public GrantCurrencyCard(int amount)
    {
        this.amount = amount;
    }

    public override string Description => $"立即获得 {Mathf.Max(0, amount)} 金币。";

    public override void OnInstall()
    {
        CurrencyWallet currencyWallet = Context?.GetComponent<CurrencyWallet>();
        if (currencyWallet == null || amount <= 0)
        {
            return;
        }

        currencyWallet.ChangeAmount(amount);
    }
}
