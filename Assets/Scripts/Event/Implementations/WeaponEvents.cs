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

public struct RequestWeaponSelectionSnapshotEvent : IGameEvent
{
}