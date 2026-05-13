public static class WeaponPriceHelper
{
    private static readonly float[] LevelPriceMultipliers =
    {
        1f,
        1.6f,
        3.6f,
        7.2f
    };

    public static int GetPrice(int basePrice, int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        float scaledPrice = basePrice * GetLevelPriceMultiplier(clampedLevel);
        return PropValueUtility.ResolveNonNegativePrice(scaledPrice);
    }

    public static float GetLevelPriceMultiplier(int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        int index = clampedLevel - WeaponLevelHelper.MinLevel;
        return LevelPriceMultipliers[index];
    }
}
