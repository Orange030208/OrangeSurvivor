public readonly struct StageCompleteSnapshot
{
    public int CompletedWaves { get; }
    public float SurvivalTime { get; }
    public int KillCount { get; }
    public int GoldEarned { get; }
    public string CharacterName { get; }
    public string MainWeaponName { get; }

    public StageCompleteSnapshot(int completedWaves, float survivalTime, int killCount, int goldEarned, string characterName, string mainWeaponName)
    {
        CompletedWaves = completedWaves;
        SurvivalTime = survivalTime;
        KillCount = killCount;
        GoldEarned = goldEarned;
        CharacterName = characterName;
        MainWeaponName = mainWeaponName;
    }
}
