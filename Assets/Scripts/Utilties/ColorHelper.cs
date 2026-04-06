using UnityEngine;

public static class ColorHelper
{
    public static Color GetColorByLevel(int level)
    {
        return level switch
        {
            1 => new Color32(172, 172, 172, 255), // 灰
            2 => new Color32(86, 186, 105, 255),  // 绿
            3 => new Color32(77, 140, 255, 255),  // 蓝
            4 => new Color32(163, 104, 255, 255), // 紫
            5 => new Color32(255, 166, 52, 255),  // 橙
            6 => new Color32(255, 86, 86, 255),   // 红
            _ => Color.white
        };
    }

    public static Color GetColorByRarity(int rarity)
    {
        return rarity switch
        {
            0 => new Color32(180, 210, 255, 255),  // 淡蓝
            1 => new Color32(210, 180, 255, 255),  // 淡紫
            2 => new Color32(255, 220, 180, 255), // 淡橙
            3 => new Color32(255, 190, 190, 255), // 淡红
            _ => Color.white
        };
    }
}
