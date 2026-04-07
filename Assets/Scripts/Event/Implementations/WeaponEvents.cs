using System;

public struct WeaponSelectionChangedEvent : IGameEvent
{
    public SelectionWeapon[] SelectionWeapons;

    public WeaponSelectionChangedEvent(SelectionWeapon[] selectionWeapons)
    {
        SelectionWeapons = selectionWeapons;
    }
}

public struct WeaponSelectionOptionClickedEvent : IGameEvent
{
    public int Index;

    public WeaponSelectionOptionClickedEvent(int index)
    {
        Index = index;
    }
}

public struct WeaponSelectionContainerClickedEvent : IGameEvent
{
    public int ContainerIndex;
    public WeaponDataSO WeaponData;
    public int Level;

    public WeaponSelectionContainerClickedEvent(int containerIndex, WeaponDataSO weaponData, int level)
    {
        ContainerIndex = containerIndex;
        WeaponData = weaponData;
        Level = level;
    }
}

public struct RequestWeaponSelectionSnapshotEvent : IGameEvent
{
}