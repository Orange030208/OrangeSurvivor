public static class PropDescriptionUtility
{
    public static string GetIconRichText(this PropType propType)
    {
        return RichTextStringUtility.GetSpriteTagByIconName(propType.ToString());
    }

    public static string GetIconRichTextWithVOffset(this PropType propType, float offset = -0.2f)
    {
        return RichTextStringUtility.WrapWithVOffsetTag(propType.GetIconRichText(), offset);
    }

    public static string GetIconNameWithRichText(this PropType propType, float iconOffset = -0.2f)
    {
        return RichTextStringUtility.Create()
            .AppendWithVOffset(propType.GetIconRichText(), iconOffset)
            .Append(propType.ToString())
            .ToString();
    }

    public static string BuildIconNameValueDescription(this PropType propType, float value, float iconOffset = -0.2f, float valuePositionPercent = 80f)
    {
        return propType.BuildIconNameValueDescription(FormatValue(propType, value), value, iconOffset, valuePositionPercent);
    }

    public static string BuildIconNameValueDescription(this PropType propType, string valueText, float rawValue, float iconOffset = -0.2f, float valuePositionPercent = 80f)
    {
        string leftContent = propType.GetIconNameWithRichText(iconOffset);
        string rightContent = ColorHelper.WrapRichTextColor(valueText, ColorHelper.GetColorByValue(rawValue));

        return RichTextStringUtility.Create()
            .AppendHeadTail(leftContent, rightContent, valuePositionPercent)
            .ToString();
    }

    public static string FormatModifierValue(this PropType propType, PropModifierType modifierType, float value)
    {
        if (modifierType == PropModifierType.BaseMultiplier
            || modifierType == PropModifierType.BonusMultiplier
            || modifierType == PropModifierType.FinalMultiplier)
        {
            return FormatSignedPercent(value);
        }

        if (IsPercentAdditiveProp(propType))
        {
            return FormatSignedPercent(value);
        }

        string formatted = IsIntegerProp(propType) ? value.ToString("F0") : value.ToString("F1");
        return value > 0 ? $"+{formatted}" : formatted;
    }

    public static string BuildModifierDescription(this PropType propType, PropModifierType modifierType, float value)
    {
        string propName = propType.ToString();
        string coloredValue = ColorHelper.WrapRichTextColor(BuildPlainValueText(propType, modifierType, value), ColorHelper.GetColorByValue(value));

        return modifierType switch
        {
            PropModifierType.Add => $"{coloredValue} {propName}",
            PropModifierType.BaseMultiplier => $"{propName} 基础乘区 {coloredValue}",
            PropModifierType.BonusMultiplier => $"{propName} 加成乘区 {coloredValue}",
            PropModifierType.FinalMultiplier => $"{propName} 最终乘区 {coloredValue}",
            _ => $"{coloredValue} {propName}"
        };
    }

    private static string BuildPlainValueText(PropType propType, PropModifierType modifierType, float value)
    {
        return modifierType switch
        {
            PropModifierType.BaseMultiplier => FormatSignedNumber(value * 100f) + "%",
            PropModifierType.BonusMultiplier => FormatSignedNumber(value * 100f) + "%",
            PropModifierType.FinalMultiplier => FormatSignedNumber(value * 100f) + "%",
            _ => IsPercentAdditiveProp(propType)
                ? FormatSignedNumber(value * 100f) + "%"
                : FormatSignedNumber(propType, value)
        };
    }

    private static string FormatValue(PropType propType, float value)
    {
        return IsPercentAdditiveProp(propType) ? FormatSignedPercent(value) : FormatSignedNumber(propType, value);
    }

    public static string FormatDisplayValue(this PropType propType, float value)
    {
        return FormatValue(propType, value);
    }

    private static bool IsPercentAdditiveProp(PropType propType)
    {
        return propType == PropType.ExperienceGain ||
               propType == PropType.ShopPriceDiscount ||
               propType == PropType.KnockbackResistance;
    }

    private static bool IsIntegerProp(PropType propType)
    {
        return propType == PropType.WeaponSlotCount ||
               propType == PropType.ProjectilePierceCount;
    }

    private static string FormatSignedPercent(float value)
    {
        float percent = value * 100f;
        string formatted = percent.ToString("F1");
        return percent > 0 ? $"+{formatted}%" : $"{formatted}%";
    }

    private static string FormatSignedNumber(float value)
    {
        string formatted = value.ToString("F1");
        return value > 0 ? $"+{formatted}" : formatted;
    }

    private static string FormatSignedNumber(PropType propType, float value)
    {
        string formatted = IsIntegerProp(propType) ? value.ToString("F0") : value.ToString("F1");
        return value > 0 ? $"+{formatted}" : formatted;
    }
}
