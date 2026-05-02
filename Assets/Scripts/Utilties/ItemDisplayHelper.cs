public static class ItemDisplayHelper
{
    public static string GetLevelPrefix(int level)
    {
        return WeaponLevelHelper.ClampLevel(level) switch
        {
            1 => "灰",
            2 => "蓝",
            3 => "紫",
            4 => "橙",
            _ => string.Empty
        };
    }

    public static string GetWeaponDisplayName(string weaponName, int level)
    {
        int displayLevel = WeaponLevelHelper.ClampLevel(level);
        return $"Lv.{displayLevel} [{GetLevelPrefix(displayLevel)}] {weaponName}";
    }
}
