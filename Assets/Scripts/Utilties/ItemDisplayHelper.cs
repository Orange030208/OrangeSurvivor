public static class ItemDisplayHelper
{
    public static string GetLevelPrefix(int level)
    {
        return level switch
        {
            1 => "灰",
            2 => "绿",
            3 => "蓝",
            4 => "紫",
            5 => "橙",
            6 => "红",
            _ => string.Empty
        };
    }

    public static string GetWeaponDisplayName(string weaponName, int level)
    {
        return $"Lv.{level} [{GetLevelPrefix(level)}] {weaponName}";
    }
}
