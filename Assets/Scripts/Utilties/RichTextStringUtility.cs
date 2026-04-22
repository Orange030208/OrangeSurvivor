using System.Text;

public static class RichTextStringUtility
{
    public static RichTextBuilder Create()
    {
        return new RichTextBuilder();
    }

    public static string GetSpriteTagByIndex(int index)
    {
        return $"<sprite={index}>";
    }

    public static string GetSpriteTagByIconName(string iconName)
    {
        return $"<sprite name=\"{iconName}\">";
    }

    public static string WrapWithVOffsetTag(string content, float offsetEm = -0.2f)
    {
        return $"<voffset={offsetEm:0.0##}em>{content}</voffset>";
    }

    public static string BuildHeadTailText(string headContent, string tailContent, float tailPositionPercent = 75f)
    {
        string safeHeadContent = headContent ?? string.Empty;
        string safeTailContent = tailContent ?? string.Empty;
        return $"{safeHeadContent}<pos={tailPositionPercent:0.##}%>{safeTailContent}";
    }

    public sealed class RichTextBuilder
    {
        private readonly StringBuilder stringBuilder = new();

        public RichTextBuilder Append(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                stringBuilder.Append(content);
            }

            return this;
        }

        public RichTextBuilder AppendSpriteByIndex(int index)
        {
            stringBuilder.Append(GetSpriteTagByIndex(index));
            return this;
        }

        public RichTextBuilder AppendSpriteByIconName(string iconName)
        {
            stringBuilder.Append(GetSpriteTagByIconName(iconName));
            return this;
        }

        public RichTextBuilder AppendWithVOffset(string content, float offsetEm = -0.2f)
        {
            stringBuilder.Append(WrapWithVOffsetTag(content, offsetEm));
            return this;
        }

        public RichTextBuilder AppendHeadTail(string headContent, string tailContent, float tailPositionPercent = 80f)
        {
            stringBuilder.Append(BuildHeadTailText(headContent, tailContent, tailPositionPercent));
            return this;
        }

        public RichTextBuilder Clear()
        {
            stringBuilder.Clear();
            return this;
        }

        public override string ToString()
        {
            return stringBuilder.ToString();
        }
    }
}
