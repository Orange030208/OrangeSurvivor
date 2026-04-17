using System.Collections.Generic;
using UnityEngine;

public enum TooltipVisualStyle
{
    Neutral = 0,
    Positive = 1,
    Negative = 2,
    Accent = 3
}

public readonly struct TooltipDisplayData
{
    public readonly string Title;
    public readonly Sprite Icon;
    public readonly IReadOnlyList<string> Descriptions;
    public readonly string Footer;
    public readonly TooltipVisualStyle VisualStyle;

    public TooltipDisplayData(
        string title,
        Sprite icon,
        IReadOnlyList<string> descriptions,
        string footer,
        TooltipVisualStyle visualStyle)
    {
        Title = title;
        Icon = icon;
        Descriptions = descriptions;
        Footer = footer;
        VisualStyle = visualStyle;
    }
}
