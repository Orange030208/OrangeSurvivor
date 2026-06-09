using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public readonly struct WeaponInfoSource
{
    public WeaponInfoSource(WeaponDataSO weaponData, int level, Weapon runtimeWeapon = null)
    {
        WeaponData = weaponData != null ? weaponData : runtimeWeapon != null ? runtimeWeapon.WeaponData : null;
        Level = WeaponLevelHelper.ClampLevel(runtimeWeapon != null ? runtimeWeapon.Level : level);
        RuntimeWeapon = runtimeWeapon;
    }

    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public Weapon RuntimeWeapon { get; }

    public static WeaponInfoSource FromData(WeaponDataSO weaponData, int level)
    {
        return new WeaponInfoSource(weaponData, level);
    }

    public static WeaponInfoSource FromRuntime(Weapon runtimeWeapon)
    {
        return new WeaponInfoSource(
            runtimeWeapon != null ? runtimeWeapon.WeaponData : null,
            runtimeWeapon != null ? runtimeWeapon.Level : WeaponLevelHelper.MinLevel,
            runtimeWeapon);
    }
}

public sealed class WeaponInfoBuilder : IInfoDocumentBuilder<WeaponInfoSource>
{
    public InfoDocument Build(WeaponInfoSource source)
    {
        WeaponDataSO weaponData = source.WeaponData;
        if (weaponData == null)
        {
            return BuildMissingDocument();
        }

        WeaponLevelStatData baseStats = weaponData.GetLevelStats(source.Level);
        Weapon runtimeWeapon = source.RuntimeWeapon;
        WeaponBenefitData baseBenefits = baseStats.StatBenefits;
        WeaponBenefitData runtimeBenefits = runtimeWeapon != null ? runtimeWeapon.Benefits : WeaponBenefitData.Zero;

        List<InfoItem> items = new()
        {
            InfoDocumentUtility.CreateTitle(weaponData.ItemName),
            InfoDocumentUtility.CreateLineBreak()
        };

        string imageKey = weaponData.WeaponId;
        if (!string.IsNullOrWhiteSpace(imageKey))
        {
            items.Add(InfoDocumentUtility.CreateImage(
                imageKey,
                new WeaponImage(weaponData.ItemIcon)));
        }

        string tagText = BuildTagText(weaponData.Tags);
        if (!string.IsNullOrWhiteSpace(tagText))
        {
            items.Add(InfoDocumentUtility.CreateTagText(tagText));
        }

        items.AddRange(BuildStatItems(baseStats, runtimeWeapon, baseBenefits, runtimeBenefits));

        List<InfoItem> holderItems = BuildHolderModifierItems(baseStats.HolderModifiers);
        if (holderItems.Count > 0)
        {
            items.AddRange(holderItems);
        }

        string manualDescription = ItemDescriptionUtility.NormalizeManualDescription(weaponData.ManualDescription);
        if (!string.IsNullOrWhiteSpace(manualDescription))
        {
            if (items.Count > 0)
            {
                items.Add(InfoDocumentUtility.CreateSpacer());
            }

            InfoDocumentUtility.AppendTextLine(items, $"\"{manualDescription}\"", InfoTone.Disabled);
        }

        return new InfoDocument(
            weaponData.WeaponId,
            items);
    }

    private static InfoDocument BuildMissingDocument()
    {
        return new InfoDocument(
            string.Empty,
            new[]
            {
                InfoDocumentUtility.CreateTitle("缺失武器数据"),
                InfoDocumentUtility.CreateLineBreak(),
                InfoDocumentUtility.CreateSectionHeader("说明"),
                InfoDocumentUtility.CreateLineBreak(),
                InfoDocumentUtility.CreateText("无法生成武器详情：WeaponDataSO 为空。", InfoTone.Warning),
                InfoDocumentUtility.CreateLineBreak()
            });
    }

    private static List<InfoItem> BuildStatItems(
        WeaponLevelStatData baseStats,
        Weapon runtimeWeapon,
        WeaponBenefitData baseBenefits,
        WeaponBenefitData runtimeBenefits)
    {
        float cooldown = PropValueUtility.AttackSpeedPointsToAttackInterval(baseStats.AttackSpeed);
        float criticalChance = baseStats.CriticalChance;
        float criticalMultiplier = PropValueUtility.PercentPointsToRatio(baseStats.CriticalPercent);
        float range = baseStats.Range;
        float knockback = baseStats.KnockbackStrength;

        List<InfoItem> items = new();
        AppendTextPropertyLine(items, PropType.Attack, BuildDamageText(baseStats.Attack, baseBenefits, runtimeBenefits), baseStats.Attack);
        AppendTextPropertyLine(items, PropType.AttackSpeed, BuildBenefitLineText($"{FormatNumber(cooldown)}s", "攻速", baseBenefits.AttackSpeedBenefitPercent, runtimeBenefits.AttackSpeedBenefitPercent), cooldown);
        AppendTextPropertyLine(items, PropType.CriticalChance, BuildBenefitLineText(FormatPercent(criticalChance), "暴击率", baseBenefits.CriticalChanceBenefitPercent, runtimeBenefits.CriticalChanceBenefitPercent), criticalChance);
        AppendTextPropertyLine(items, PropType.CriticalPercent, BuildBenefitLineText($"x{FormatNumber(criticalMultiplier)}", "暴击伤害", baseBenefits.CriticalPercentBenefitPercent, runtimeBenefits.CriticalPercentBenefitPercent), criticalMultiplier);
        AppendTextPropertyLine(items, PropType.AttackRange, BuildBenefitLineText(FormatNumber(range), "攻击范围", baseBenefits.RangeBenefitPercent, runtimeBenefits.RangeBenefitPercent), range);

        if (knockback > 0f || baseStats.KnockbackStrength > 0f)
        {
            AppendTextPropertyLine(
                items,
                PropType.KnockbackStrength,
                BuildBenefitLineText(FormatNumber(knockback), "击退", baseBenefits.KnockbackStrengthBenefitPercent, runtimeBenefits.KnockbackStrengthBenefitPercent),
                knockback);
        }

        return items;
    }

    private static void AppendTextPropertyLine(List<InfoItem> items, PropType propType, string valueText, float rawValue)
    {
        InfoDocumentUtility.AppendPropertyLine(
            items,
            propType.ToString(),
            valueText,
            rawValue > 0f ? InfoTone.Positive : rawValue < 0f ? InfoTone.Negative : InfoTone.Neutral);
    }

    private static string BuildDamageText(float baseDamage, WeaponBenefitData baseBenefits, WeaponBenefitData runtimeBenefits)
    {
        string attackUsageText = BuildAttackUsageText(baseBenefits, runtimeBenefits);
        return string.IsNullOrWhiteSpace(attackUsageText)
            ? FormatNumber(baseDamage)
            : $"{FormatNumber(baseDamage)} <color=#{ColorUtility.ToHtmlStringRGB(new Color32(135, 145, 155, 255))}>(</color><color=#{ColorUtility.ToHtmlStringRGB(new Color32(91, 214, 255, 255))}>{attackUsageText}</color><color=#{ColorUtility.ToHtmlStringRGB(new Color32(135, 145, 155, 255))}>)</color>";
    }

    private static string BuildBenefitLineText(
        string valueText,
        string contributionLabel,
        params float[] benefitPercents)
    {
        string contributionText = BuildContributionText(contributionLabel, benefitPercents);
        return string.IsNullOrWhiteSpace(contributionText)
            ? valueText
            : $"{valueText} <color=#{ColorUtility.ToHtmlStringRGB(new Color32(135, 145, 155, 255))}>(</color><color=#{ColorUtility.ToHtmlStringRGB(new Color32(79, 220, 111, 255))}>{contributionText}</color><color=#{ColorUtility.ToHtmlStringRGB(new Color32(135, 145, 155, 255))}>)</color>";
    }

    private static List<InfoItem> BuildHolderModifierItems(IReadOnlyList<PropModifierData> modifiers)
    {
        List<InfoItem> items = new();
        if (modifiers == null)
        {
            return items;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            PropModifierData modifier = modifiers[i];
            InfoDocumentUtility.AppendTextLine(
                items,
                $"持有者{modifier.GetDisplayName()}: {modifier.GetDisplayValueText()}",
                modifier.value >= 0f ? InfoTone.Positive : InfoTone.Negative);
        }

        return items;
    }

    private static string BuildContributionText(string contributionLabel, params float[] benefitPercents)
    {
        List<string> contributions = new();
        if (benefitPercents == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < benefitPercents.Length; i++)
        {
            float benefitPercent = benefitPercents[i];
            if (benefitPercent <= 0f)
            {
                continue;
            }

            contributions.Add($"{FormatNumber(benefitPercent)}%{contributionLabel}");
        }

        return contributions.Count > 0 ? string.Join(" + ", contributions) : string.Empty;
    }

    private static string BuildTagText(IReadOnlyList<WeaponTag> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        List<string> labels = new();
        for (int i = 0; i < tags.Count; i++)
        {
            labels.Add(FormatWeaponTag(tags[i]));
        }

        return string.Join(" / ", labels);
    }

    private static string BuildAttackUsageText(WeaponBenefitData baseBenefits, WeaponBenefitData runtimeBenefits)
    {
        List<string> parts = new();
        AddAttackUsageText(parts, "近战", baseBenefits.MeleeAttackUsagePercent, runtimeBenefits.MeleeAttackUsagePercent);
        AddAttackUsageText(parts, "远程", baseBenefits.RangedAttackUsagePercent, runtimeBenefits.RangedAttackUsagePercent);
        AddAttackUsageText(parts, "魔法", baseBenefits.MagicAttackUsagePercent, runtimeBenefits.MagicAttackUsagePercent);
        AddAttackUsageText(parts, "召唤", baseBenefits.SummonAttackUsagePercent, runtimeBenefits.SummonAttackUsagePercent);
        return parts.Count > 0 ? string.Join(" + ", parts) : string.Empty;
    }

    private static void AddAttackUsageText(List<string> parts, string label, float baseUsagePercent, float runtimeUsagePercent)
    {
        float totalUsagePercent = baseUsagePercent + runtimeUsagePercent;
        if (totalUsagePercent <= 0f)
        {
            return;
        }

        List<string> contributions = new();
        if (baseUsagePercent > 0f)
        {
            contributions.Add($"{FormatNumber(baseUsagePercent)}%{label}");
        }

        if (runtimeUsagePercent > 0f)
        {
            contributions.Add($"{FormatNumber(runtimeUsagePercent)}%{label}");
        }

        parts.Add(string.Join(" + ", contributions));
    }

    private static string FormatPercent(float value)
    {
        return $"{FormatNumber(value)}%";
    }

    private static string FormatWeaponTag(WeaponTag tag)
    {
        return tag switch
        {
            WeaponTag.Heavy => "重型",
            WeaponTag.Fast => "快速",
            WeaponTag.Growth => "成长",
            WeaponTag.Precision => "精准",
            _ => tag.ToString()
        };
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
