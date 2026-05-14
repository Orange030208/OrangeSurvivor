using System;

public enum RewardSelectionReason
{
    None,
    Chest,
    Upgrade,
    Weapon
}

public struct UpgradeRewardAvailableEvent : IGameEvent
{
    public int UnspentUpgradePoints;

    public UpgradeRewardAvailableEvent(int unspentUpgradePoints)
    {
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}
