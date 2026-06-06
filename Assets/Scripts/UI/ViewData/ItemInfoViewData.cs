public readonly struct ItemInfoViewData
{
    public readonly string Name;
    public readonly string TypeText;
    public readonly string TagText;
    public readonly string BodyRichText;

    public ItemInfoViewData(string name, string typeText, string tagText, string bodyRichText)
    {
        Name = name ?? string.Empty;
        TypeText = typeText ?? string.Empty;
        TagText = tagText ?? string.Empty;
        BodyRichText = bodyRichText ?? string.Empty;
    }

    public string GetMetaText()
    {
        if (string.IsNullOrWhiteSpace(TypeText))
        {
            return TagText ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(TagText))
        {
            return TypeText;
        }

        return $"{TypeText} / {TagText}";
    }
}
