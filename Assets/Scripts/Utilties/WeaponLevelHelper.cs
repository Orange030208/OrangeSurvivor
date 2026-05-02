public static class WeaponLevelHelper
{
    public const int MinLevel = 1;
    public const int MaxLevel = 4;

    public static int ClampLevel(int level)
    {
        if (level < MinLevel)
        {
            return MinLevel;
        }

        if (level > MaxLevel)
        {
            return MaxLevel;
        }

        return level;
    }

    public static bool IsMaxLevel(int level)
    {
        return level >= MaxLevel;
    }

    public static bool CanMerge(int level)
    {
        return level >= MinLevel && level < MaxLevel;
    }

    public static bool TryGetMergedLevel(int currentLevel, out int mergedLevel)
    {
        if (!CanMerge(currentLevel))
        {
            mergedLevel = ClampLevel(currentLevel);
            return false;
        }

        mergedLevel = currentLevel + 1;
        return true;
    }

    public static int GetRandomLevelInclusiveMax()
    {
        return UnityEngine.Random.Range(MinLevel, MaxLevel + 1);
    }
}
