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

public struct RewardSelectionCardSelectedEvent : IGameEvent
{
    public string RequestId;
    public int OptionIndex;
    public string OptionId;

    public RewardSelectionCardSelectedEvent(string requestId, int optionIndex, string optionId)
    {
        RequestId = requestId ?? string.Empty;
        OptionIndex = optionIndex;
        OptionId = optionId ?? string.Empty;
    }
}
