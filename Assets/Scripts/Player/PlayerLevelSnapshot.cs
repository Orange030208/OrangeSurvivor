public readonly struct PlayerLevelSnapshot
{
    public int CurrentLevel { get; }
    public int CurrentXP { get; }
    public int RequiredXP { get; }
    public int UnspentUpgradePoints { get; }

    public PlayerLevelSnapshot(int currentLevel, int currentXP, int requiredXP, int unspentUpgradePoints)
    {
        CurrentLevel = currentLevel;
        CurrentXP = currentXP;
        RequiredXP = requiredXP;
        UnspentUpgradePoints = unspentUpgradePoints;
    }
}
