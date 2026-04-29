using System;

public struct UpgradeOptionsChangedEvent : IGameEvent
{
    public UpgradeCardOptionSnapshot[] Options;

    public UpgradeOptionsChangedEvent(UpgradeCardOptionSnapshot[] options)
    {
        Options = options;
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
