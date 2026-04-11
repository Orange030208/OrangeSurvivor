using UnityEngine;

public static class ColorHelper
{
    public static Color GetColorByLevel(int level)
    {
        return WeaponLevelHelper.ClampLevel(level) switch
        {
            1 => new Color32(172, 172, 172, 255),
            2 => new Color32(86, 186, 105, 255),
            3 => new Color32(77, 140, 255, 255),
            4 => new Color32(163, 104, 255, 255),
            5 => new Color32(255, 166, 52, 255),
            6 => new Color32(255, 86, 86, 255),
            _ => Color.white
        };
    }

    public static Color GetColorByRarity(int rarity)
    {
        return rarity switch
        {
            0 => new Color32(180, 210, 255, 255),
            1 => new Color32(210, 180, 255, 255),
            2 => new Color32(255, 220, 180, 255),
            3 => new Color32(255, 190, 190, 255),
            _ => Color.white
        };
    }

    public static Color GetColorByValue(float value)
    {
        if (value < 0) return Color.red;
        if (value > 0) return Color.green;
        return Color.white;
    }

    public static string WrapRichTextColor(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
