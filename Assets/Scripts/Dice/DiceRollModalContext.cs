/// <summary>
/// 打开骰子结算 Modal 时显式传入已确定的结果；表现层不会再次随机。
/// </summary>
public sealed class DiceRollModalContext
{
    public DiceRollModalContext(DiceRollResult result)
    {
        Result = result;
    }

    public DiceRollResult Result { get; }
}
