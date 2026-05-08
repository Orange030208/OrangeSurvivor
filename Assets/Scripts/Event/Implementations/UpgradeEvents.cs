using System;

public enum RewardSelectionReason
{
    None,
    Chest,
    Upgrade
}

public enum RewardSelectionPhase
{
    None,
    ChestSelection,
    UpgradeSelection
}

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

public struct UpgradeCardsRefreshOutRequestedEvent : IGameEvent
{
}

public struct UpgradeCardsRefreshOutCompletedEvent : IGameEvent
{
}

public struct UpgradeRewardAvailableEvent : IGameEvent
{
    public int UnspentUpgradePoints;

    public UpgradeRewardAvailableEvent(int unspentUpgradePoints)
    {
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}

public struct RewardSelectionCompletedEvent : IGameEvent
{
}
