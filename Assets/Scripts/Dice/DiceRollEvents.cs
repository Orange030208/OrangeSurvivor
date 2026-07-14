/// <summary>
/// 骰子视觉结算完成后发送的类型事件。
/// </summary>
public readonly struct DiceRolledEvent
{
    public DiceRolledEvent(DiceRollResult result)
    {
        Result = result;
    }

    public DiceRollResult Result { get; }
}
