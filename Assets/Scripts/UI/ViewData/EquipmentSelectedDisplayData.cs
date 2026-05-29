using UnityEngine;

public readonly struct EquipmentSelectedDisplayData
{
    public readonly Sprite Icon;
    public readonly string Name;

    public EquipmentSelectedDisplayData(Sprite icon, string name)
    {
        Icon = icon;
        Name = name;
    }
}
