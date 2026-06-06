using System;

[Obsolete("Use ItemInfoViewData instead.")]
public readonly struct EquipmentInfoDocumentViewData
{
    public readonly string Name;
    public readonly string TypeText;
    public readonly string DescriptionText;

    public EquipmentInfoDocumentViewData(string name, string typeText, string descriptionText)
    {
        Name = name ?? string.Empty;
        TypeText = typeText ?? string.Empty;
        DescriptionText = descriptionText ?? string.Empty;
    }
}
