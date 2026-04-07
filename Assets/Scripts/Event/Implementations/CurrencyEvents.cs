using System;

public struct CurrencyChangedEvent : IGameEvent
{
    /// <summary>
    /// 改变之后的货币数量
    /// </summary>
    public int CurrentCurrency;
    public int ChangeAmount;

    public CurrencyChangedEvent(int currentCurrency, int changeAmount)
    {
        CurrentCurrency = currentCurrency;
        ChangeAmount = changeAmount;
    }
}

/// <summary>
/// 拉取一次新的货币
/// </summary>
public struct FetchCurrencyEvent : IGameEvent
{
}