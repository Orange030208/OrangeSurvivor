using System;

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public PropModifierData[] PropEntries;

    public UpgradeOptionsChangedEvent(PropModifierData[] propEntries)
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

public struct UpgradeSelectionCompletedEvent : IGameEvent
{
}
