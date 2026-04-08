using System;

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public PropEntry[] PropEntries
        ;

    public UpgradeOptionsChangedEvent(PropEntry[] propEntries)
    {
        PropEntries = propEntries;
    }
}

public struct UpgradeContainerClickedEvent : IGameEvent
{
    public int ContainerIndex;

    public UpgradeContainerClickedEvent(int containerIndex)
    {
        ContainerIndex = containerIndex;
    }
}