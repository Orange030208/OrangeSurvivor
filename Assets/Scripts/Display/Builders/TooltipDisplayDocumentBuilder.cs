using System.Collections.Generic;
using UnityEngine;

public static class TooltipDisplayDocumentBuilder
{
    private static readonly PropEntryDisplayBuilder propEntryDisplayBuilder = new();

    public static DisplayDocument CreateFromItem(ItemDataSO itemData, int colorDependencyNumber)
    {
        if (itemData == null)
        {
            return new DisplayDocument();
        }

        switch (itemData)
        {
            case WeaponDataSO weaponData:
                return new DisplayDocument
                {
                    Id = $"tooltip_weapon_{weaponData.ItemName}_{colorDependencyNumber}",
                    Title = ItemDisplayHelper.GetWeaponDisplayName(weaponData.ItemName, colorDependencyNumber),
                    Icon = weaponData.ItemIcon,
                    Footer = $"等级 {colorDependencyNumber}",
                    Blocks = new DisplayBlock[]
                    {
                        propEntryDisplayBuilder.BuildTextBlock(weaponData.GetPropEntriesByLevel(colorDependencyNumber), new DisplayContext { IsCompact = true })
                    }
                };
            case AccessoryDataSO accessoryData:
                return new DisplayDocument
                {
                    Id = $"tooltip_accessory_{accessoryData.AccessoryId}",
                    Title = accessoryData.ItemName,
                    Icon = accessoryData.ItemIcon,
                    Footer = $"稀有度 {accessoryData.Rarity + 1}",
                    Blocks = accessoryData.BuildDisplayDocument().Blocks
                };
            default:
                return new DisplayDocument
                {
                    Id = $"tooltip_item_{itemData.ItemName}",
                    Title = itemData.ItemName,
                    Icon = itemData.ItemIcon
                };
        }
    }

    public static DisplayDocument CreateFromBuff(ActiveBuffSnapshot buffSnapshot)
    {
        string footer = buffSnapshot.HasDuration
            ? $"{buffSnapshot.StackCount}/{buffSnapshot.MaxStackCount} 层 · {buffSnapshot.RemainingDurationSeconds:0.0}s"
            : $"{buffSnapshot.StackCount}/{buffSnapshot.MaxStackCount} 层 · 常驻";

        return new DisplayDocument
        {
            Id = $"tooltip_buff_{buffSnapshot.BuffId}",
            Title = buffSnapshot.DisplayName,
            Icon = buffSnapshot.Icon,
            Footer = footer,
            Blocks = buffSnapshot.Document != null ? buffSnapshot.Document.Blocks : System.Array.Empty<DisplayBlock>()
        };
    }

    public static TextListBlock CreateTextBlock(IReadOnlyList<string> descriptions)
    {
        if (descriptions == null || descriptions.Count == 0)
        {
            return new TextListBlock
            {
                BlockId = "tooltip_descriptions",
                Items = System.Array.Empty<TextLineItem>()
            };
        }

        List<TextLineItem> items = new(descriptions.Count);
        for (int i = 0; i < descriptions.Count; i++)
        {
            string description = descriptions[i];
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            items.Add(new TextLineItem
            {
                Text = description,
                StyleKey = "default"
            });
        }

        return new TextListBlock
        {
            BlockId = "tooltip_descriptions",
            Items = items
        };
    }
}
