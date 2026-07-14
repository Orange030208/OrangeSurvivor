using System;

/// <summary>
/// 打开骰子弹窗时显式传入已确定的结果；表现层不会再次随机。
/// </summary>
public sealed class DiceRollPopupContext
{
    public DiceRollPopupContext(DiceRollResult result, Action<DiceRollResult> completed = null)
    {
        Result = result;
        Completed = completed;
    }

    public DiceRollResult Result { get; }
    public Action<DiceRollResult> Completed { get; }
}
