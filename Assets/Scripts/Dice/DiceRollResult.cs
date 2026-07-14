using System;

/// <summary>
/// 不可变的骰子结算结果。点数在创建时校验，避免无效结果流入表现或玩法层。
/// </summary>
public readonly struct DiceRollResult : IEquatable<DiceRollResult>
{
    public const int MIN_FACE_VALUE = 1;
    public const int MAX_FACE_VALUE = 6;

    public DiceRollResult(int faceValue)
    {
        if (faceValue < MIN_FACE_VALUE || faceValue > MAX_FACE_VALUE)
        {
            throw new ArgumentOutOfRangeException(nameof(faceValue), faceValue,
                $"Dice face value must be between {MIN_FACE_VALUE} and {MAX_FACE_VALUE}.");
        }

        FaceValue = faceValue;
    }

    public int FaceValue { get; }

    public bool Equals(DiceRollResult other)
    {
        return FaceValue == other.FaceValue;
    }

    public override bool Equals(object obj)
    {
        return obj is DiceRollResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return FaceValue;
    }

    public override string ToString()
    {
        return FaceValue.ToString();
    }
}
