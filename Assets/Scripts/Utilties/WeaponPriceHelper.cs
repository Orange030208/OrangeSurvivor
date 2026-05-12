using Mathf = UnityEngine.Mathf;

public static class WeaponPriceHelper
{
    private static readonly float[] LevelPriceMultipliers =
    {
        1f,
        1.8f,
        3.8f,
        7.8f
    };

    public static int GetPrice(int basePrice, int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        float scaledPrice = basePrice * GetLevelPriceMultiplier(clampedLevel);
        return Mathf.RoundToInt(scaledPrice);
    }

    public static float GetLevelPriceMultiplier(int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        int index = clampedLevel - WeaponLevelHelper.MinLevel;
        return LevelPriceMultipliers[index];
    }
}
