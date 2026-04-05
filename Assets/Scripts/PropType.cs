using System;

public enum PropType
{
    Attack,
    AttackSpeed,
    CriticalChance,
    CriticalPercent,
    MoveSpeed,
    MaxHealth,
    Range,
    HealthRecoverySpeed,
    Armor,
    Luck,
    Dodge,
    LifeSteal
}

public static class PropTypeExtensions
{
    public static string GetChineseName(this PropType propType)
    {
        return propType switch
        {
            PropType.Attack => "攻击力",
            PropType.AttackSpeed => "攻击速度",
            PropType.CriticalChance => "暴击率",
            PropType.CriticalPercent => "暴击伤害",
            PropType.MoveSpeed => "移动速度",
            PropType.MaxHealth => "最大生命值",
            PropType.Range => "攻击范围",
            PropType.HealthRecoverySpeed => "生命恢复速度",
            PropType.Armor => "护甲",
            PropType.Luck => "幸运",
            PropType.Dodge => "闪避",
            PropType.LifeSteal => "生命偷取",
            _ => "未知属性"
        };
    }

    public static string FormatPropName(this PropType propType)
    {
        string unformatted = propType.ToString();
        if (string.IsNullOrEmpty(unformatted)) return "非法属性";

        // 栈上分配缓冲区
        int maxLength = unformatted.Length * 2;
        Span<char> buffer = stackalloc char[maxLength];
        int pos = 0;

        buffer[pos++] = unformatted[0];

        foreach (char c in unformatted.AsSpan(1))
        {
            if (c is >= 'A' and <= 'Z') buffer[pos++] = ' ';
            buffer[pos++] = c;
        }

        return new string(buffer.Slice(0, pos));
    }
}