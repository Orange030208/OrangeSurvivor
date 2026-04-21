public static class PropDescriptionUtility
{
    public static string GetIcon(this PropType propType)
    {
        return RichTextStringUtility.GetSpriteTagByIconName(propType.ToString());
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
            PropType.Range => "攻击范围",
            PropType.HealthRecoverySpeed => "生命恢复速度",
            PropType.Armor => "护甲",
            PropType.Luck => "幸运",
            PropType.Dodge => "闪避",
            PropType.LifeSteal => "生命偷取",
            PropType.PickupRadius => "拾取半径",
            PropType.ProjectileCount => "弹体数量",
            PropType.ProjectileSpeed => "弹体速度",
            PropType.CooldownReduction => "冷却缩减",
            PropType.SkillDuration => "持续时间",
            PropType.AreaSize => "范围尺寸",
            PropType.KnockbackForce => "击退强度",
            PropType.StatusChance => "异常附加率",
            PropType.ExperienceGain => "经验获取",
            PropType.GoldGain => "金币获取",
            PropType.ShopPriceModifier => "商店价格修正",
            PropType.EnemySpawnWeightModifier => "敌人密度修正",
            PropType.ReviveCount => "复活次数",
            PropType.DamageReduction => "伤害减免",
            PropType.HealingPower => "治疗效果",
            PropType.ThornsDamage => "反伤",
            PropType.Curse => "诅咒",
            PropType.MagnetStrength => "吸附强度",
            _ => "未知属性"
        };
    }

    public static float GetDefaultValue(this PropType propType)
    {
        return propType switch
        {
            PropType.Attack => 0f,
            PropType.AttackSpeed => 1f,
            PropType.CriticalChance => 0f,
            PropType.CriticalPercent => 0f,
            PropType.MoveSpeed => 2f,
            PropType.MaxHealth => 100f,
            PropType.Range => 0f,
            PropType.HealthRecoverySpeed => 0f,
            PropType.Armor => 0f,
            PropType.Luck => 0f,
            PropType.Dodge => 0f,
            PropType.LifeSteal => 0f,
            PropType.PickupRadius => 1f,
            PropType.ProjectileCount => 0f,
            PropType.ProjectileSpeed => 1f,
            PropType.CooldownReduction => 0f,
            PropType.SkillDuration => 1f,
            PropType.AreaSize => 1f,
            PropType.KnockbackForce => 0f,
            PropType.StatusChance => 0f,
            PropType.ExperienceGain => 1f,
            PropType.GoldGain => 1f,
            PropType.ShopPriceModifier => 1f,
            PropType.EnemySpawnWeightModifier => 1f,
            PropType.ReviveCount => 0f,
            PropType.DamageReduction => 0f,
            PropType.HealingPower => 1f,
            PropType.ThornsDamage => 0f,
            PropType.Curse => 0f,
            PropType.MagnetStrength => 1f,
            _ => 0f
        };
    }

    public static string FormatModifierValue(this PropType propType, PropModifierType modifierType, float value)
    {
        if (modifierType == PropModifierType.BasePercent || modifierType == PropModifierType.FinalPercent)
        {
            return FormatSignedPercent(value);
        }

        string formatted = value.ToString("F1");
        return value > 0 ? $"+{formatted}" : formatted;
    }

    public static string BuildModifierDescription(this PropType propType, PropModifierType modifierType, float value)
    {
        string propName = propType.GetChineseName();
        string coloredValue = ColorHelper.WrapRichTextColor(BuildPlainValueText(modifierType, value), ColorHelper.GetColorByValue(value));

        return modifierType switch
        {
            PropModifierType.Flat => $"{coloredValue} {propName}",
            PropModifierType.BasePercent => $"{coloredValue} {propName}",
            PropModifierType.FinalFlat => $"{propName} 最终固定修正 {coloredValue}",
            PropModifierType.FinalPercent => $"{propName} 的最终修正为 {coloredValue}",
            _ => $"{coloredValue} {propName}"
        };
    }

    private static string BuildPlainValueText(PropModifierType modifierType, float value)
    {
        return modifierType switch
        {
            PropModifierType.BasePercent => FormatSignedNumber(value * 100f) + "%",
            PropModifierType.FinalPercent => value.ToString("F1") + "%",
            _ => FormatSignedNumber(value)
        };
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
}