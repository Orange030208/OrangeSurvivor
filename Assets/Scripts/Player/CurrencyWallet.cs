using System;
using UnityEngine;

/// <summary>
/// 基础货币钱包组件，可挂载到任意实体或场景对象上。
/// </summary>
public class CurrencyWallet : MonoBehaviour
{
    [SerializeField] private int initialAmount;

    private int currentAmount;

    public int CurrentAmount => currentAmount;

    public event Action<int, int> OnAmountChanged;

    private void Awake()
    {
        currentAmount = initialAmount;
    }

    public void ChangeAmount(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        currentAmount += amount;
        OnAmountChanged?.Invoke(currentAmount, amount);
        GameEventBus.Publish(new CurrencyChangedEvent(this, currentAmount, amount));
    }

    public void SetAmount(int amount)
    {
        int changeAmount = amount - currentAmount;
        if (changeAmount == 0)
        {
            return;
        }

        currentAmount = amount;
        OnAmountChanged?.Invoke(currentAmount, changeAmount);
        GameEventBus.Publish(new CurrencyChangedEvent(this, currentAmount, changeAmount));
    }
}
