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
            .Append(propType.GetChineseName())
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

    public static string GetChineseName(this PropType propType)
    {
        return propType switch
        {
            PropType.Attack => "攻击力",
            PropType.AttackSpeed => "攻击速度",
            PropType.CriticalChance => "暴击率",
            PropType.CriticalPercent => "暴击伤害",
            PropType.MoveSpeed => "移动速度",
            PropType.MaxHealth => "最大生命值",
            PropType.DetectionRange => "检测范围",
            PropType.AttackRange => "攻击范围",
            PropType.HealthRecoverySpeed => "生命恢复速度",
            PropType.Armor => "护甲",
            PropType.Luck => "幸运",
            PropType.Dodge => "闪避",
            PropType.LifeSteal => "生命偷取",
            PropType.PickupRadius => "拾取半径",
            PropType.ProjectileCount => "弹体数量",
            PropType.ProjectileSpeed => "弹体速度",
            PropType.ProjectilePierceCount => "弹射物穿透数量",
            PropType.WeaponSlotCount => "武器槽位数量",
            PropType.KnockbackForce => "击退强度",
            PropType.ExperienceGain => "经验获取",
            PropType.ShopPriceDiscount => "商店折扣",
            PropType.WaveGoldRewardBonus => "波次金币奖励",
            PropType.DamageReduction => "伤害减免",
            PropType.HealingPower => "治疗效果",
            _ => "未知属性"
        };
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
        string propName = propType.GetChineseName();
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

    private static bool IsPercentAdditiveProp(PropType propType)
    {
        return propType == PropType.ExperienceGain ||
               propType == PropType.ShopPriceDiscount;
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
