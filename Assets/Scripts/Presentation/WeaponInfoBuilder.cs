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
        return new WeaponInfoSource(runtimeWeapon != null ? runtimeWeapon.WeaponData : null, runtimeWeapon != null ? runtimeWeapon.Level : WeaponLevelHelper.MinLevel, runtimeWeapon);
    }
}

public sealed class WeaponInfoBuilder : IInfoDocumentBuilder<WeaponInfoSource>
{
    private const string UntitledSectionTitle = "";

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

        List<InfoSection> sections = new()
        {
            new InfoSection(UntitledSectionTitle, BuildStatLines(baseStats, runtimeWeapon, baseBenefits, runtimeBenefits))
        };

        List<InfoLine> holderLines = BuildHolderModifierLines(baseStats.HolderModifiers);
        if (holderLines.Count > 0)
        {
            sections.Add(new InfoSection(UntitledSectionTitle, holderLines));
        }

        string manualDescription = ItemDescriptionUtility.NormalizeManualDescription(weaponData.ManualDescription);
        if (!string.IsNullOrWhiteSpace(manualDescription))
        {
            sections.Add(new InfoSection(
                UntitledSectionTitle,
                new[] { InfoDocumentUtility.CreateSingleValueLine(string.Empty, manualDescription) }));
        }

        return new InfoDocument(
            weaponData.WeaponId,
            weaponData.ItemName,
            weaponData.ItemIcon,
            InfoDocumentKind.Weapon,
            BuildTagLabels(weaponData.Tags),
            sections);
    }

    private static InfoDocument BuildMissingDocument()
    {
        return new InfoDocument(
            string.Empty,
            "缺失武器数据",
            null,
            InfoDocumentKind.Weapon,
            Array.Empty<string>(),
            new[]
            {
                new InfoSection(
                    UntitledSectionTitle,
                    new[] { InfoDocumentUtility.CreateSingleValueLine(string.Empty, "无法生成武器详情：WeaponDataSO 为空。", InfoTone.Warning) })
            });
    }

    private static List<InfoLine> BuildStatLines(
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

        List<InfoLine> lines = new()
        {
            BuildDamageLine(baseStats.Attack, baseBenefits, runtimeBenefits),
            BuildBenefitLine("冷却", $"{FormatNumber(cooldown)}s", "攻速", baseBenefits.AttackSpeedBenefitPercent, runtimeBenefits.AttackSpeedBenefitPercent),
            BuildBenefitLine("暴击率", FormatPercent(criticalChance), "暴击率", baseBenefits.CriticalChanceBenefitPercent, runtimeBenefits.CriticalChanceBenefitPercent),
            BuildBenefitLine("暴击伤害", $"x{FormatNumber(criticalMultiplier)}", "暴击伤害", baseBenefits.CriticalPercentBenefitPercent, runtimeBenefits.CriticalPercentBenefitPercent),
            BuildBenefitLine("范围", FormatRange(range), "攻击范围", baseBenefits.RangeBenefitPercent, runtimeBenefits.RangeBenefitPercent)
        };

        if (knockback > 0f || baseStats.KnockbackStrength > 0f)
        {
            lines.Add(BuildBenefitLine("击退", FormatNumber(knockback), "击退", baseBenefits.KnockbackStrengthBenefitPercent, runtimeBenefits.KnockbackStrengthBenefitPercent));
        }

        return lines;
    }

    private static InfoLine BuildDamageLine(float baseDamage, WeaponBenefitData baseBenefits, WeaponBenefitData runtimeBenefits)
    {
        List<InfoLinePart> parts = new()
        {
            InfoDocumentUtility.Text(FormatNumber(baseDamage))
        };

        string attackUsageText = BuildAttackUsageText(baseBenefits, runtimeBenefits);
        if (!string.IsNullOrWhiteSpace(attackUsageText))
        {
            parts.Add(InfoDocumentUtility.Text(" (", InfoTone.Disabled));
            parts.Add(InfoDocumentUtility.Text(attackUsageText, InfoTone.Emphasis));
            parts.Add(InfoDocumentUtility.Text(")", InfoTone.Disabled));
        }

        return new InfoLine("伤害", parts);
    }

    private static InfoLine BuildBenefitLine(
        string label,
        string valueText,
        string contributionLabel,
        params float[] benefitPercents)
    {
        List<InfoLinePart> parts = new()
        {
            InfoDocumentUtility.Text(valueText)
        };

        string contributionText = BuildContributionText(contributionLabel, benefitPercents);
        if (!string.IsNullOrWhiteSpace(contributionText))
        {
            parts.Add(InfoDocumentUtility.Text(" (", InfoTone.Disabled));
            parts.Add(InfoDocumentUtility.Text(contributionText, InfoTone.Positive));
            parts.Add(InfoDocumentUtility.Text(")", InfoTone.Disabled));
        }

        return new InfoLine(label, parts);
    }

    private static List<InfoLine> BuildHolderModifierLines(IReadOnlyList<PropModifierData> modifiers)
    {
        List<InfoLine> lines = new();
        if (modifiers == null)
        {
            return lines;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            PropModifierData modifier = modifiers[i];
            lines.Add(InfoDocumentUtility.CreateSingleValueLine(
                modifier.GetDisplayName(),
                modifier.GetDisplayValueText(),
                modifier.value >= 0f ? InfoTone.Positive : InfoTone.Negative));
        }

        return lines;
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

    private static List<string> BuildTagLabels(IReadOnlyList<WeaponTag> tags)
    {
        List<string> labels = new();
        if (tags == null)
        {
            return labels;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            labels.Add(FormatWeaponTag(tags[i]));
        }

        return labels;
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

    private static string FormatRange(float value)
    {
        return FormatNumber(value);
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
