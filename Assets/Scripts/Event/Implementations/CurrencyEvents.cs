using System;

public struct CurrencyChangedEvent : IGameEvent
{
    /// <summary>
    /// 发生变化的货币类型
    /// </summary>
    public CurrencyType CurrencyType;

    /// <summary>
    /// 改变之后的货币数量
    /// </summary>
    public int CurrentAmount;

    public int ChangeAmount;

    public CurrencyChangedEvent(CurrencyType currencyType, int currentAmount, int changeAmount)
    {
        CurrencyType = currencyType;
        CurrentAmount = currentAmount;
        ChangeAmount = changeAmount;
    }
}

/// <summary>
/// 请求一次指定货币类型的快照。
/// </summary>
public struct RequestCurrencySnapshotEvent : IGameEvent
{
    public CurrencyType CurrencyType;

    public RequestCurrencySnapshotEvent(CurrencyType currencyType)
    {
        CurrencyType = currencyType;
    }
}

/// <summary>
/// 请求变更指定货币类型。
/// </summary>
public struct CurrencyChangeRequestedEvent : IGameEvent
{
    public CurrencyType CurrencyType;
    public int Amount;

    public CurrencyChangeRequestedEvent(CurrencyType currencyType, int amount)
    {
        CurrencyType = currencyType;
        Amount = amount;
    }
}
