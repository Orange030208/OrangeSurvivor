using System;

public enum RewardSelectionReason
{
    None,
    Chest,
    Upgrade
}

public struct UpgradeRewardAvailableEvent : IGameEvent
{
    public int UnspentUpgradePoints;

    public UpgradeRewardAvailableEvent(int unspentUpgradePoints)
    {
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}
