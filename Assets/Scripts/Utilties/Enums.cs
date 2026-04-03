using System;

public static class Enums
{
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

        return new string(buffer.Slice(0,pos));
    }
}