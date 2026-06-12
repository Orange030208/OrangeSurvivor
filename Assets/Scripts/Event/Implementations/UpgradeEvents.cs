using System;

public enum RewardSelectionReason
{
    None,
    Chest,
    Upgrade,
    Weapon
}

public enum RewardTrigger
{
    ChestCollected
}

public struct UpgradeRewardAvailableEvent
{
    public int UnspentUpgradePoints;

    public UpgradeRewardAvailableEvent(int unspentUpgradePoints)
    {
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}
