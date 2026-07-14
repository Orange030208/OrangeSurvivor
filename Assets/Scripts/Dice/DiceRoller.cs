using System;

/// <summary>
/// 骰子规则仅负责产出点数，不持有任何 Unity 表现资源。
/// </summary>
public sealed class DiceRoller
{
    private readonly IContentRandom random;

    public DiceRoller(IContentRandom random = null)
    {
        this.random = random ?? new UnityContentRandom();
    }

    public DiceRollResult Roll()
    {
        int faceValue = random.Range(DiceRollResult.MIN_FACE_VALUE, DiceRollResult.MAX_FACE_VALUE + 1);
        return new DiceRollResult(faceValue);
    }
}
