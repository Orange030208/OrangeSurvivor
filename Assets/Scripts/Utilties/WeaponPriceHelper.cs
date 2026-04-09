using Mathf = UnityEngine.Mathf;

public static class WeaponPriceHelper
{
    private const float LevelPriceMultiplier = 1.5f;

    public static int GetPrice(int basePrice, int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        float scaledPrice = basePrice * Mathf.Pow(LevelPriceMultiplier, clampedLevel - WeaponLevelHelper.MinLevel);
        return Mathf.RoundToInt(scaledPrice);
    }
}
