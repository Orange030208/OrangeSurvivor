using System;
using UnityEngine;

public class CurrencyManager : MonoSingletonBase<CurrencyManager>
{
    [field: SerializeField] public int Currency { get; private set; }

    private void OnEnable()
    {
        GameEventBus.Subscribe<FetchCurrencyEvent>(FetchCurrency);
        // 启用时立即发布当前货币数据，确保UI能获取到初始状态
        GameEventBus.Publish(new CurrencyChangedEvent(Currency, 0));
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<FetchCurrencyEvent>(FetchCurrency);
    }

    private void FetchCurrency()
    {
        GameEventBus.Publish(new CurrencyChangedEvent(Currency, 0));
    }

    public void AddCurrency(int amount)
    {
        Currency += amount;
        GameEventBus.Publish(new CurrencyChangedEvent(Currency, amount));
    }
}