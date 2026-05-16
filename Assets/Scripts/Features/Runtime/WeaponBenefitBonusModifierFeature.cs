using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeaponBenefitBonusModifierFeature : FeatureBase
{
    [SerializeField] private WeaponBenefitData benefitBonus;
    private string runtimeSourceId;

    public WeaponBenefitBonusModifierFeature()
    {
    }

    public WeaponBenefitBonusModifierFeature(WeaponBenefitData benefitBonus)
    {
        this.benefitBonus = benefitBonus.Validated();
    }

    public override string Title => "武器收益率加成";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        runtimeSourceId = ResolveRuntimeSourceId();
        weaponsHolder.AddWeaponBenefitModifier(runtimeSourceId, benefitBonus.Validated());
    }

    public override void OnUninstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        weaponsHolder.RemoveWeaponBenefitModifier(ResolveRuntimeSourceId());
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        AddBenefitInfo(infos, PropType.AttackSpeed, benefitBonus.AttackSpeedBenefitPercent);
        AddBenefitInfo(infos, PropType.CriticalChance, benefitBonus.CriticalChanceBenefitPercent);
        AddBenefitInfo(infos, PropType.CriticalPercent, benefitBonus.CriticalPercentBenefitPercent);
        AddBenefitInfo(infos, PropType.AttackRange, benefitBonus.RangeBenefitPercent);
        AddBenefitInfo(infos, PropType.KnockbackStrength, benefitBonus.KnockbackStrengthBenefitPercent);
        AddBenefitInfo(infos, PropType.MeleeAttack, benefitBonus.MeleeAttackUsagePercent);
        AddBenefitInfo(infos, PropType.RangedAttack, benefitBonus.RangedAttackUsagePercent);
        AddBenefitInfo(infos, PropType.MagicAttack, benefitBonus.MagicAttackUsagePercent);
        AddBenefitInfo(infos, PropType.SummonAttack, benefitBonus.SummonAttackUsagePercent);
        return infos;
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(WeaponBenefitBonusModifierFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(WeaponBenefitBonusModifierFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        List<string> parts = new();
        AddBenefitDescription(parts, PropType.AttackSpeed, benefitBonus.AttackSpeedBenefitPercent);
        AddBenefitDescription(parts, PropType.CriticalChance, benefitBonus.CriticalChanceBenefitPercent);
        AddBenefitDescription(parts, PropType.CriticalPercent, benefitBonus.CriticalPercentBenefitPercent);
        AddBenefitDescription(parts, PropType.AttackRange, benefitBonus.RangeBenefitPercent);
        AddBenefitDescription(parts, PropType.KnockbackStrength, benefitBonus.KnockbackStrengthBenefitPercent);
        AddBenefitDescription(parts, PropType.MeleeAttack, benefitBonus.MeleeAttackUsagePercent);
        AddBenefitDescription(parts, PropType.RangedAttack, benefitBonus.RangedAttackUsagePercent);
        AddBenefitDescription(parts, PropType.MagicAttack, benefitBonus.MagicAttackUsagePercent);
        AddBenefitDescription(parts, PropType.SummonAttack, benefitBonus.SummonAttackUsagePercent);
        return parts.Count > 0
            ? $"所有武器{string.Join("，", parts)}。"
            : string.Empty;
    }

    private static void AddBenefitInfo(List<DescriptorInfo> infos, PropType propType, float benefitPercent)
    {
        if (benefitPercent <= 0f)
        {
            return;
        }

        infos.Add(new DescriptorInfo(
            $"{GameContentRuntime.GetPropDisplayName(propType)}收益率",
            $"+{benefitPercent:0.##}%"));
    }

    private static void AddBenefitDescription(List<string> parts, PropType propType, float benefitPercent)
    {
        if (benefitPercent <= 0f)
        {
            return;
        }

        parts.Add($"{GameContentRuntime.GetPropDisplayName(propType)}收益率 +{benefitPercent:0.##}%");
    }
}
