using UnityEngine;

public static class TooltipDataFactory
{
    public static TooltipDisplayData CreateFromItem(ItemDataSO itemData, int colorDependencyNumber)
    {
        if (itemData == null)
        {
            return new TooltipDisplayData(string.Empty, null, null, string.Empty, TooltipVisualStyle.Neutral);
        }

        return itemData switch
        {
            WeaponDataSO weaponData => new TooltipDisplayData(
                ItemDisplayHelper.GetWeaponDisplayName(weaponData.ItemName, colorDependencyNumber),
                weaponData.ItemIcon,
                weaponData.GetDescriptions(colorDependencyNumber),
                $"等级 {colorDependencyNumber}",
                TooltipVisualStyle.Accent),
            AccessoryDataSO accessoryData => new TooltipDisplayData(
                accessoryData.ItemName,
                accessoryData.ItemIcon,
                accessoryData.GetDescriptions(),
                $"稀有度 {accessoryData.Rarity + 1}",
                TooltipVisualStyle.Accent),
            _ => new TooltipDisplayData(itemData.ItemName, itemData.ItemIcon, null, string.Empty, TooltipVisualStyle.Neutral)
        };
    }

    public static TooltipDisplayData CreateFromBuff(ActiveBuffSnapshot buffSnapshot)
    {
        TooltipVisualStyle visualStyle = buffSnapshot.Polarity switch
        {
            BuffPolarity.Positive => TooltipVisualStyle.Positive,
            BuffPolarity.Negative => TooltipVisualStyle.Negative,
            _ => TooltipVisualStyle.Neutral
        };

        string footer = buffSnapshot.HasDuration
            ? $"{buffSnapshot.StackCount}/{buffSnapshot.MaxStackCount} 层 · {buffSnapshot.RemainingDurationSeconds:0.0}s"
            : $"{buffSnapshot.StackCount}/{buffSnapshot.MaxStackCount} 层 · 常驻";

        return new TooltipDisplayData(
            buffSnapshot.DisplayName,
            buffSnapshot.Icon,
            buffSnapshot.Descriptions,
            footer,
            visualStyle);
    }
}
