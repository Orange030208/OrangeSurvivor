using UnityEngine;

public readonly struct EquipmentInfoDocumentViewData
{
    public readonly Sprite Icon;
    public readonly string Name;
    public readonly string TypeText;
    public readonly string DescriptionText;

    public EquipmentInfoDocumentViewData(Sprite icon, string name, string typeText, string descriptionText)
    {
        Icon = icon;
        Name = name;
        TypeText = typeText;
        DescriptionText = descriptionText;
    }
}
