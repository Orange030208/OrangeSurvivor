using UnityEngine;

public struct CurrencyChangedEvent : IGameEvent
{
    /// <summary>
    /// 发生变化的钱包实例。
    /// </summary>
    public CurrencyWallet Wallet;

    /// <summary>
    /// 改变之后的货币数量。
    /// </summary>
    public int CurrentAmount;

    public int ChangeAmount;

    public CurrencyChangedEvent(CurrencyWallet wallet, int currentAmount, int changeAmount)
    {
        Wallet = wallet;
        CurrentAmount = currentAmount;
        ChangeAmount = changeAmount;
    }
}
