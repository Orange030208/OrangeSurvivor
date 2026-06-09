using UnityEngine;

public static class ItemNameStyleUtility
{
    public static string GetWeaponDisplayName(string weaponName, int level)
    {
        return GetWeaponDisplayName(weaponName, ContentTierResolver.FromWeaponLevel(level));
    }

    public static string GetWeaponDisplayName(string weaponName, ContentTier tier)
    {
        return GetTierColoredName(weaponName, tier);
    }

    public static string GetAccessoryDisplayName(string accessoryName, ContentTier tier)
    {
        return GetTierColoredName(accessoryName, tier);
    }

    private static string GetTierColoredName(string itemName, ContentTier tier)
    {
        string resolvedName = itemName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            return string.Empty;
        }

        string colorHex = ColorUtility.ToHtmlStringRGB(GameContentRuntime.GetTierColor(tier));
        return $"<color=#{colorHex}>{resolvedName}</color>";
    }
}
