public static class RichTextStringUtility
{
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
}
