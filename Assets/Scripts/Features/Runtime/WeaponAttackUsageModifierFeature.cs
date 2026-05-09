using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeaponAttackUsageModifierFeature : FeatureEffectBase
{
    [SerializeField] private WeaponAttackUsageData attackUsageBonus;
    private string runtimeSourceId;

    public WeaponAttackUsageModifierFeature()
    {
    }

    public WeaponAttackUsageModifierFeature(WeaponAttackUsageData attackUsageBonus)
    {
        this.attackUsageBonus = attackUsageBonus.Validated();
    }

    public override string Title => "武器攻击使用区加成";
    public override string Description => BuildDescription();

    public override void OnInstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        runtimeSourceId = ResolveRuntimeSourceId();
        weaponsHolder.AddWeaponAttackUsageModifier(runtimeSourceId, attackUsageBonus);
    }

    public override void OnUninstall()
    {
        WeaponsHolder weaponsHolder = Context?.GetComponent<WeaponsHolder>();
        if (weaponsHolder == null)
        {
            return;
        }

        weaponsHolder.RemoveWeaponAttackUsageModifier(ResolveRuntimeSourceId());
    }

    public override IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        List<DescriptorInfo> infos = new();
        AddUsageInfo(infos, PropType.MeleeAttack, attackUsageBonus.MeleeAttackUsagePercent);
        AddUsageInfo(infos, PropType.RangedAttack, attackUsageBonus.RangedAttackUsagePercent);
        AddUsageInfo(infos, PropType.MagicAttack, attackUsageBonus.MagicAttackUsagePercent);
        AddUsageInfo(infos, PropType.SummonAttack, attackUsageBonus.SummonAttackUsagePercent);
        return infos;
    }

    private string ResolveRuntimeSourceId()
    {
        if (!string.IsNullOrWhiteSpace(runtimeSourceId))
        {
            return runtimeSourceId;
        }

        return string.IsNullOrWhiteSpace(SourceId)
            ? $"{nameof(WeaponAttackUsageModifierFeature)}_{GetHashCode()}"
            : $"{SourceId}:{nameof(WeaponAttackUsageModifierFeature)}_{GetHashCode()}";
    }

    private string BuildDescription()
    {
        List<string> parts = new();
        AddUsageDescription(parts, PropType.MeleeAttack, attackUsageBonus.MeleeAttackUsagePercent);
        AddUsageDescription(parts, PropType.RangedAttack, attackUsageBonus.RangedAttackUsagePercent);
        AddUsageDescription(parts, PropType.MagicAttack, attackUsageBonus.MagicAttackUsagePercent);
        AddUsageDescription(parts, PropType.SummonAttack, attackUsageBonus.SummonAttackUsagePercent);
        return parts.Count > 0
            ? $"所有武器{string.Join("，", parts)}。"
            : string.Empty;
    }

    private static void AddUsageInfo(List<DescriptorInfo> infos, PropType propType, float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            return;
        }

        infos.Add(new DescriptorInfo(
            $"{GameContentRuntime.GetPropDisplayName(propType)}使用区",
            $"+{usagePercent:0.##}%"));
    }

    private static void AddUsageDescription(List<string> parts, PropType propType, float usagePercent)
    {
        if (usagePercent <= 0f)
        {
            return;
        }

        parts.Add($"{GameContentRuntime.GetPropDisplayName(propType)}使用区 +{usagePercent:0.##}%");
    }
}
