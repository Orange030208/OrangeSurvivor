using UnityEngine;

public static class ColorHelper
{
    public static Color GetColorByLevel(int level)
    {
        return ItemQualityVisualResolver.Resolve(ItemType.Weapon, level).PrimaryColor;
    }

    public static Color GetColorByTier(ContentTier tier)
    {
        return ItemQualityVisualResolver.Resolve(ItemType.Accessory, (int)tier).PrimaryColor;
    }

    public static Color GetColorByValue(float value)
    {
        if (value < 0)
        {
            return Color.red;
        }

        if (value > 0)
        {
            return Color.green;
        }

        return Color.white;
    }

    public static string WrapRichTextColor(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
